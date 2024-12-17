using System.Linq;
using System.Numerics;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Mind;
using Content.Server.Preferences.Managers;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.MapGen;
using Content.Server.Theta.MapGen.Prototypes;
using Content.Server.Theta.MobHUD;
using Content.Server.Theta.NiceColors;
using Content.Server.Theta.NiceColors.ColorPalettes;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Preferences;
using Content.Shared.Projectiles;
using Content.Shared.Roles.Theta;
using Content.Shared.Shuttles.Events;
using Content.Shared.Theta.MobHUD;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.Misc.GenericWarningUI;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;
using Robust.Shared.Utility;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Mind.Components;
using Content.Shared.Ghost;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using System.Collections;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventTeamSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly ActionsSystem _actSys = default!;
    [Dependency] private readonly ChatSystem _chatSys = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly MobHUDSystem _hudSys = default!;
    [Dependency] private readonly MapGenSystem _mapGenSys = default!;
    [Dependency] private readonly IdentitySystem _idSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly TransformSystem _formSys = default!;
    [Dependency] private readonly RoundEndSystem _endSys = default!;
    [Dependency] private readonly MindSystem _mindSys = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanAppearanceSys = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsMan = default!;
    [Dependency] private readonly ITimerManager _timerMan = default!;
    [Dependency] private readonly PhysicsSystem _physSys = default!;

    private readonly Dictionary<string, int> _projectileDamage = new(); //cached damage for projectile prototypes
    private int _lastTeamNumber;
    public float RoundendTimer;
    private int _lastAnnoucementMinute;

    //all time-related fields are specified in seconds
    public float RoundDuration;
    public bool TimedRoundEnd = false;

    public float TeamCheckInterval;
    public float PlayerCheckInterval;
    public float RespawnDelay;
    public int MaxSpawnOffset;

    public int BonusInterval;
    public int PointsPerInterval; //points for surviving longer than BonusInterval without respawn

    public float PointsPerHitMultiplier;
    public int PointsPerAssist;
    public int PointsPerKill;
    public int OutOfBoundsPenalty; //subtracted from team's points every update cycle while they're out of bounds

    public const string HUDPrototypeId = "ShipeventHUD";
    public const string CaptainHUDPrototypeId = "ShipeventHUDCaptain";
    public const string TeamChannelID = "Common";
    public const string FleetChannelID = "Fleet";
    public const string TeamViewActionPrototype = "ShipEventTeamViewToggle";
    public const string CaptainMenuActionPrototype = "ShipEventCaptainMenuToggle";
    public const string ReturnToLobbyActionPrototype = "ShipEventReturnToLobbyAction";

    private bool _ruleSelected = false;
    public bool RuleSelected
    {
        get => _ruleSelected;
        set
        {
            _ruleSelected = value;
            if (value)
                OnRuleSelected?.Invoke();
        }
    }

    public List<ShipTypePrototype> ShipTypes = new();
    public MapId TargetMap;
    public List<IMapGenProcessor> ShipProcessors = new();

    public List<ShipEventTeam> Teams { get; } = new();

    public ColorPalette ColorPalette = new ShipEventPalette();
    public bool AllowTeamRegistration = true;
    public bool AllowPlayerRespawn = true;
    public bool RemoveEmptyTeams = true;

    public Action? OnRuleSelected;
    public Action<RoundEndTextAppendEvent>? RoundEndEvent;
    //used by modifiers to prevent locking subscriptions
    public Action<EntityUid>? OnPlayerSpawn;
    public Action<EntityUid, MobState>? OnPlayerStateChange;

    public CancellationTokenSource TimerTokenSource = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundEndDiscordTextAppendEvent>(OnRoundEndDiscord);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<ShipEventTeamMarkerComponent, ShipEventTeamViewToggleEvent>(OnViewToggle);
        SubscribeLocalEvent<ShipEventTeamMarkerComponent, ShipEventCaptainMenuToggleEvent>(OnCapMenuToggle);

        SubscribeLocalEvent<ShipEventReturnToLobbyEvent>(OnReturnToLobbyAction);
        SubscribeLocalEvent<GenericWarningYesPressedMessage>(OnReturnPlayerToLobby);

        SubscribeLocalEvent<ShipEventTeamMarkerComponent, StartCollideEvent>(OnCollision);
        SubscribeLocalEvent<ShipEventTeamMarkerComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<ShipEventTeamMarkerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ShipEventTeamMarkerComponent, EntitySpokeEvent>(OnTeammateSpeak);
        SubscribeLocalEvent<ShipEventTeamMarkerComponent, MobStateChangedEvent>(OnMobStateChange);

        SubscribeLocalEvent<ShipEventSpawnerComponent, ComponentShutdown>(OnSpawnerDestroyed);
        SubscribeLocalEvent<ShipEventPointStorageComponent, UseInHandEvent>(OnPointStorageTriggered);

        SubscribeAllEvent<ShuttleConsoleChangeShipNameMessage>(OnShipNameChange); //un-directed event since we will have duplicate subscriptions otherwise
        SubscribeAllEvent<GetShipPickerInfoMessage>(OnShipPickerInfoRequest);
        SubscribeAllEvent<BoundsOverlayInfoRequest>(OnBoundsOverlayInfoRequest);

        InitializeAnomalies();
        InitializeStealth();
        InitializeCaptainMenu();

        OnRuleSelected += SetupTimers;
    }

    public override void Update(float frametime)
    {
        if (!RuleSelected)
            return;

        RoundendTimer += frametime;
        CheckRoundendTimer();
    }

    private void SetupTimers()
    {
        SetupTimer(TeamCheckInterval, CheckTeams);
        SetupTimer(PlayerCheckInterval, CheckPlayers);
        SetupTimer(BoundsCompressionInterval, BoundsUpdate);
        SetupTimer(PickupSpawnInterval, PickupSpawn);
        SetupTimer(AnomalyUpdateInterval, AnomalyUpdate);
        SetupTimer(AnomalySpawnInterval, AnomalySpawn);
        SetupTimer(ModifierUpdateInterval, ModifierUpdate);
    }

    private void SetupTimer(float seconds, Action action)
    {
        if (seconds <= 0)
            return;
        Timer timer = new((int) (seconds * 1000), true, action);
        _timerMan.AddTimer(timer, TimerTokenSource.Token);
    }

    #region Round end

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _lastTeamNumber = 0;
        RoundendTimer = 0;
        _lastAnnoucementMinute = 0;
        CurrentBoundsOffset = 0;

        RuleSelected = false;
        AllowTeamRegistration = true;
        RemoveEmptyTeams = true;

        TargetMap = MapId.Nullspace;

        ShipTypes.Clear();
        ShipProcessors.Clear();
        AnomalyPrototypes.Clear();
        PickupPositions.Clear();
        Teams.Clear();

        DisableAllModifiers();

        TimerTokenSource.Cancel();
        TimerTokenSource.Dispose();
        TimerTokenSource = new();
    }

    private void CheckRoundendTimer()
    {
        if (!TimedRoundEnd)
            return;

        var remaining = RoundDuration - RoundendTimer;
        if (remaining <= 60 * 10 && _lastAnnoucementMinute == 0)
        {
            Announce(Loc.GetString("shipevent-roundendtimer-tenmins"));
            _lastAnnoucementMinute = 10;
            return;
        }
        if (remaining <= 60 * 5 && _lastAnnoucementMinute == 10)
        {
            Announce(Loc.GetString("shipevent-roundendtimer-fivemins"));
            _lastAnnoucementMinute = 5;
            return;
        }
        if (remaining <= 60 && _lastAnnoucementMinute == 5)
        {
            Announce(Loc.GetString("shipevent-roundendtimer-onemin"));
            _lastAnnoucementMinute = 1;
            return;
        }
        if (remaining <= 0 && _lastAnnoucementMinute == 1)
        {
            _endSys.EndRound();
            _lastAnnoucementMinute = -1;
        }
    }

    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
        if (!RuleSelected || Teams.Count == 0)
            return;

        RoundEndEvent?.Invoke(args);

        var sortedTeams = Teams.ShallowClone().OrderByDescending(t => t.Points).ToList();

        var winner = sortedTeams.First();
        args.AddLine(Loc.GetString("shipevent-roundend-heading"));
        foreach (var team in sortedTeams)
        {
            args.AddLine(Loc.GetString("shipevent-roundend-team",
                ("name", team.Name),
                ("color", team.Color),
                ("shipname", team.ShipName),
                ("capname", team.Captain ?? "NONE")));
            args.AddLine(Loc.GetString("shipevent-roundend-teamstats",
                ("points", team.Points),
                ("kills", team.Kills),
                ("assists", team.Assists),
                ("respawns", team.Respawns)));
            args.AddLine("");
        }

        args.AddLine(Loc.GetString("shipevent-roundend-winner", ("name", winner.Name)));
    }

    private void OnRoundEndDiscord(ref RoundEndDiscordTextAppendEvent args)
    {
        if (!RuleSelected || Teams.Count == 0)
            return;

        var winner = Teams.First();
        foreach (var team in Teams)
        {
            if (team.Points > winner.Points)
                winner = team;
        }

        args.AddLine(Loc.GetString("shipevent-roundend-discord-team", ("capname", winner.Captain ?? "NONE")));
        args.AddLine(Loc.GetString("shipevent-roundend-discord-teamstats",
            ("points", winner.Points),
            ("kills", winner.Kills),
            ("assists", winner.Assists),
            ("respawns", winner.Respawns)
        ));
    }

    #endregion

    #region UI

    private void SetupActions(ICommonSession session, ShipEventTeam team)
    {
        if (session.AttachedEntity == null)
            return;

        EntityUid uid = session.AttachedEntity.Value;
        if (!TryComp<ShipEventActionStorageComponent>(uid, out var actStorage))
            return;

        if (actStorage.TeamViewActionUid == null)
            actStorage.TeamViewActionUid = _actSys.AddAction(uid, TeamViewActionPrototype);

        if (actStorage.ReturnToLobbyActionUid == null && HasComp<GhostComponent>(uid))
            actStorage.ReturnToLobbyActionUid = _actSys.AddAction(uid, ReturnToLobbyActionPrototype);

        if (actStorage.CaptainMenuActionUid == null && session.Channel.UserName == team.Captain)
        {
            actStorage.CaptainMenuActionUid = _actSys.AddAction(uid, CaptainMenuActionPrototype);
        }
        else if (actStorage.CaptainMenuActionUid != null && session.Channel.UserName != team.Captain)
        {
            _actSys.RemoveAction(actStorage.CaptainMenuActionUid);
            actStorage.CaptainMenuActionUid = null;
        }
    }

    private void OnViewToggle(EntityUid uid, ShipEventTeamMarkerComponent marker, ShipEventTeamViewToggleEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        if (!_playerMan.TryGetSessionByEntity(uid, out var session))
            return;

        args.Handled = true;

        List<TeamViewTeamState> teamInfo = new();
        foreach (var team in Teams)
        {
            teamInfo.Add(new TeamViewTeamState
            {
                Name = team.Name,
                Color = team.Color,
                ShipName = team.QueuedForRespawn ? null : team.ShipName,
                AliveCrewCount = team.QueuedForRespawn ? null : GetTeamLivingMembers(team).Count().ToString(),
                Points = team.Points,
            });
        }

        Enum uiKey = TeamViewUiKey.Key;
        if (_uiSys.IsUiOpen(uid, uiKey))
            return;
        _uiSys.OpenUi(uid, uiKey, session);
        _uiSys.SetUiState(uid, uiKey, new TeamViewBoundUserInterfaceState(teamInfo, ActiveModifiers.Select(m => Loc.GetString(m.Name)).ToList()));
    }

    private void OnCapMenuToggle(EntityUid uid, ShipEventTeamMarkerComponent marker, ShipEventCaptainMenuToggleEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        if (!_playerMan.TryGetSessionByEntity(uid, out var session))
            return;

        args.Handled = true;

        Enum uiKey = CaptainMenuUiKey.Key;
        if (_uiSys.IsUiOpen(uid, uiKey))
            return;

        foreach (var team in Teams)
        {
            if (team.Captain != session.Channel.UserName)
                continue;

            _uiSys.SetUiState(uid, uiKey, new ShipEventCaptainMenuBoundUserInterfaceState(
                    team.Members,
                    team.ChosenShipType,
                    team.JoinPassword,
                    team.MaxMembers));
            break;
        }

        _uiSys.OpenUi(uid, uiKey, session);
    }

    private void OnShipPickerInfoRequest(GetShipPickerInfoMessage msg)
    {
        if (!RuleSelected)
            return;

        _uiSys.SetUiState(GetEntity(msg.Entity),
            msg.UiKey,
            new ShipPickerBoundUserInterfaceState(ShipTypes));
    }

    private void OnReturnToLobbyAction(ShipEventReturnToLobbyEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        if (!_playerMan.TryGetSessionByEntity(args.Performer, out var session))
            return;

        _uiSys.SetUiState(args.Performer, GenericWarningUiKey.ShipEventKey, new GenericWarningBoundUserInterfaceState
        {
            WarningLoc = "generic-warning-window-warning-to-lobby",
        });
        _uiSys.OpenUi(args.Performer, GenericWarningUiKey.ShipEventKey, session);
    }

    #endregion

    #region Player spawning and transfer

    //I hate mind code and so I'm just gonna do everything I need in this loop
    //instead of creating 999 handlers for deaths, visits, ghosting, suicides and other shit which will never work like they should
    private void CheckPlayers()
    {
        foreach (ShipEventTeam team in Teams)
        {
            foreach (ICommonSession session in GetTeamSessions(team))
            {
                if (CompOrNull<MetaDataComponent>(session.AttachedEntity)?.EntityPrototype?.ID == "AdminObserver")
                    continue;

                if (!TryComp<MobStateComponent>(session.AttachedEntity, out var state) || state.CurrentState == MobState.Dead)
                {
                    if (!TrySpawnPlayer(session, team, out _) && HasComp<GhostComponent>(session.AttachedEntity))
                    {
                        if (TryComp<ShipEventTeamMarkerComponent>(session.AttachedEntity, out var marker) && marker.Team == null)
                        {
                            marker.Team = team;
                            SetupActions(session, team);
                        }
                    }
                }
            }
        }
    }

    //just to clean up corpses
    private void OnMindRemoved(EntityUid uid, ShipEventTeamMarkerComponent marker, MindRemovedMessage args)
    {
        if (!RuleSelected)
            return;

        QueueDel(uid);
    }

    private void OnMindAdded(Entity<ShipEventTeamMarkerComponent> uid, ref MindAddedMessage args)
    {
        if (!HasComp<GhostComponent>(uid))
            OnPlayerSpawn?.Invoke(uid);
    }

    private void OnMobStateChange(Entity<ShipEventTeamMarkerComponent> uid, ref MobStateChangedEvent args)
    {
        OnPlayerStateChange?.Invoke(uid, args.NewMobState);
    }

    //todo: our generic y/n dialog window is very weird
    private void OnReturnPlayerToLobby(GenericWarningYesPressedMessage args)
    {
        if (!Equals(args.UiKey, GenericWarningUiKey.ShipEventKey))
            return;

        var entityUid = GetEntity(args.Entity);
        if (!_playerMan.TryGetSessionByEntity(entityUid, out var session))
            return;

        if (TryComp<ShipEventTeamMarkerComponent>(entityUid, out var marker) && marker.Team != null)
        {
            if (session.Channel.UserName == marker.Team.Captain)
                AssignCaptain(marker.Team, null);

            marker.Team.Members.Remove(session.Channel.UserName);
        }
        _ticker.Respawn(session);
    }

    private bool TrySpawnPlayer(ICommonSession session, ShipEventTeam team, [NotNullWhen(true)] out EntityUid? uid, EntityUid? spawnerUid = null, bool bypass = false)
    {
        uid = null;

        if (!AllowPlayerRespawn && !bypass)
            return false;

        if (team.ShipGrids.Count == 0)
            return false;

        spawnerUid ??= GetGridCompHolders<ShipEventSpawnerComponent>(team.ShipGrids).FirstOrNull();
        if (spawnerUid == null)
            return false;

        uid = Spawn(Comp<ShipEventSpawnerComponent>(spawnerUid.Value).Prototype, Transform(spawnerUid.Value).Coordinates);
        Comp<ShipEventTeamMarkerComponent>(uid.Value).Team = team;
        if (!team.Members.Contains(session.Channel.UserName))
        {
            team.Members.Add(session.Channel.UserName);
            _chatMan.DispatchServerMessage(session, Loc.GetString("shipevent-role-greet"));
        }

        //mind transfer
        EntityUid? lastUid = session.AttachedEntity;
        _mindSys.TransferTo(_mindSys.GetOrCreateMind(session.UserId), uid, true);
        Del(lastUid);

        //appearance and name setup
        HumanoidCharacterProfile profile;
        if (_prefsMan.TryGetCachedPreferences(session.UserId, out var prefs))
        {
            profile = (HumanoidCharacterProfile) prefs.GetProfile(prefs.SelectedCharacterIndex);
        }
        else
        {
            profile = HumanoidCharacterProfile.Random();
        }
        _humanAppearanceSys.LoadProfile(uid.Value, profile);
        SetPlayerCharacterName(uid.Value, $"{GetName(uid.Value)} ({team.Name})");

        SetupHUD(session, team);
        SetupActions(session, team);

        return true;
    }

    #endregion

    #region Team creation, removal, respawn

    private void CheckTeams()
    {
        List<ShipEventTeam> emptyTeams = new();
        foreach (var team in Teams)
        {
            var activeMembers = GetTeamSessions(team);
            var livingMembers = GetTeamLivingMembers(team);

            if (RemoveEmptyTeams && activeMembers.Count == 0)
            {
                emptyTeams.Add(team);
                continue;
            }

            if (activeMembers.Count > 0 && livingMembers.Count == 0 && !team.QueuedForRespawn)
            {
                QueueTeamRespawn(team, Loc.GetString("shipevent-respawn-dead"));
                continue;
            }

            if (GetGridCompHolders<ShuttleConsoleComponent>(team.ShipGrids).Count == 0 && !team.QueuedForRespawn)
            {
                QueueTeamRespawn(team, Loc.GetString("shipevent-respawn-tech"));
                continue;
            }

            if (IsTeamOutOfBounds(team))
            {
                if (!team.OutOfBoundsWarningReceived)
                {
                    TeamMessage(team, Loc.GetString("shipevent-outofbounds"), color: Color.DarkRed);
                    team.OutOfBoundsWarningReceived = true;
                }
                PunishOutOfBoundsTeam(team);
            }
            else
            {
                team.OutOfBoundsWarningReceived = false;
            }

            //if cap is disconnected we won't be able to get his session, thus triggering this condition
            if (!team.CaptainLocked && activeMembers.Count != 0 &&
                (team.Captain == null || !_playerMan.TryGetSessionByUsername(team.Captain, out _)))
            {
                AssignCaptain(team, activeMembers[0]);
            }

            if (team.QueuedForRespawn && team.TimeSinceRemoval > RespawnDelay)
            {
                TeamMessage(team, Loc.GetString("shipevent-team-respawnnow"));
                ImmediateTeamRespawn(team);
                team.LastBonusInterval = 0;
            }

            if (Math.Floor((team.TimeSinceRemoval - RespawnDelay) / BonusInterval - team.LastBonusInterval) > 0)
            {
                TeamMessage(team,
                   Loc.GetString("shipevent-team-bonusinterval",
                        ("time", BonusInterval / 60),
                        ("points", PointsPerInterval)));
                team.Points += PointsPerInterval;
                team.LastBonusInterval++;
            }

            team.TimeSinceRemoval += TeamCheckInterval;
        }

        foreach (var team in emptyTeams)
        {
            RemoveTeam(team, Loc.GetString("shipevent-remove-noplayers"));
        }
    }

    /// <summary>
    /// Spawns ship and sets all related fields in team
    /// </summary>
    private void SpawnShip(ShipEventTeam team, string? shipName = null)
    {
        team.ChosenShipType ??= Pick(ShipTypes.Where(t => t.MinCrewAmount == 1));
        var shipGrids = _mapGenSys.RandomPosSpawn(
            TargetMap,
            new Vector2(CurrentBoundsOffset, CurrentBoundsOffset),
            MaxSpawnOffset - CurrentBoundsOffset,
            50,
            _protMan.Index<StructurePrototype>(team.ChosenShipType.StructurePrototype),
            ShipProcessors,
            true);
        team.ShipGrids = shipGrids.ToList();

        if (shipName != null)
            team.ShipName = shipName;

        //searching for grid with shuttle console on it, since it's probably the main grid and should be the one
        //with it's own name
        foreach (EntityUid gridUid in shipGrids)
        {
            var consoles = GetGridCompHolders<ShuttleConsoleComponent>(gridUid);
            if (consoles.Count != 0)
            {
                team.ShipMainGrid = gridUid;
                SetName(gridUid, team.ShipName);
                break;
            }
        }
    }

    /// <summary>
    /// Does everything needed to create a new team, from faction creation to ship spawning.
    /// </summary>
    public void CreateTeam(ICommonSession session, string name, ShipTypePrototype? initialShipType, string? password, int maxMembers, bool noCaptain = false)
    {
        if (!RuleSelected || !AllowTeamRegistration)
            return;

        Color color = ColorPalette.GetNextColor();
        ShipEventTeam team = new(IsValidName(name) ? name : GenerateTeamName(), color, noCaptain ? null : session.Channel.UserName, password)
        {
            ChosenShipType = initialShipType,
            JoinPassword = password,
            MaxMembers = maxMembers
        };
        SpawnShip(team, name);
        SetMarkers(team);

        if (!noCaptain)
        {
            AssignCaptain(team, session);
            TrySpawnPlayer(session, team, out _, bypass: true);
        }

        Teams.Add(team);
    }

    /// <summary>
    /// Adds player to faction (by name), spawns him on ship & does all other necessary stuff
    /// </summary>
    /// <param name="session">player's session</param>
    /// <param name="team">team's faction</param>
    /// <param name="password">password of the team</param>
    public void JoinTeam(ICommonSession session, ShipEventTeam team, string? password)
    {
        if (team.JoinPassword != password)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-incorrect-password"), session);
            return;
        }

        if (team.MaxMembers != 0 && team.Members.Count >= team.MaxMembers)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-memberlimit"), session);
            return;
        }

        if (team.ShipGrids.Count == 0)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-ship-destroyed"), session);
            return;
        }

        var spawners = GetGridCompHolders<ShipEventSpawnerComponent>(team.ShipGrids);
        if (spawners.Count == 0)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-spawner-destroyed-join"), session);
            return;
        }

        if (TrySpawnPlayer(session, team, out var uid, bypass: true))
            TeamMessage(team, Loc.GetString("shipevent-team-newmember", ("name", GetName(uid.Value))));
    }

    /// <summary>
    ///     Deletes current team ship & members, and marks it for respawn
    /// </summary>
    /// <param name="team">Team to respawn</param>
    /// <param name="respawnReason">Message to show in team message</param>
    /// <param name="immediate">If this team should be respawned without delay</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on respawned one</param>
    private void QueueTeamRespawn(ShipEventTeam team, string respawnReason = "", bool immediate = false, bool killPoints = true)
    {
        var message = Loc.GetString(
            "shipevent-team-respawn",
            ("respawnreason", respawnReason == "" ? Loc.GetString("shipevent-respawn-default") : respawnReason),
            ("respawntime", RespawnDelay / 60));
        TeamMessage(team, message);

        if (killPoints)
            AddKillPoints(team);
        team.Hits.Clear();

        DetachEnemiesFromGrid(team.ShipGrids, team);
        foreach (EntityUid gridUid in team.ShipGrids)
        {
            QueueDel(gridUid);
        }
        team.ShipGrids.Clear();
        team.ShipMainGrid = null;

        foreach (ICommonSession session in GetTeamSessions(team))
        {
            if (session.AttachedEntity != null && !HasComp<GhostComponent>(session.AttachedEntity))
                QueueDel(session.AttachedEntity);
        }

        if (immediate)
        {
            ImmediateTeamRespawn(team);
        }
        else
        {
            team.QueuedForRespawn = true;
            team.TimeSinceRemoval = 0;
        }
    }

    private void ImmediateTeamRespawn(ShipEventTeam team)
    {
        SpawnShip(team);
        SetMarkers(team);

        foreach (var session in GetTeamSessions(team))
        {
            TrySpawnPlayer(session, team, out _, bypass: true);
        }

        team.Respawns++;
        team.QueuedForRespawn = false;
    }

    /// <summary>
    /// Removes team entirely
    /// </summary>
    /// <param name="team">Team to remove</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on removed one</param>
    public void RemoveTeam(ShipEventTeam team, string removalReason = "", bool killPoints = true)
    {
        var message = Loc.GetString(
            "shipevent-team-remove",
            ("removereason", removalReason == "" ? Loc.GetString("shipevent-remove-default") : removalReason));
        TeamMessage(team, message);

        if (killPoints)
            AddKillPoints(team);

        foreach (var session in GetTeamSessions(team))
        {
            QueueDel(session.AttachedEntity);
        }

        foreach (EntityUid gridUid in team.ShipGrids)
        {
            QueueDel(gridUid);
        }

        Teams.Remove(team);
    }

    #endregion

    #region Points

    private void OnCollision(EntityUid entity, ShipEventTeamMarkerComponent component, ref StartCollideEvent args)
    {
        if (!RuleSelected || component.Team == null)
            return;

        if (EntityManager.HasComponent<ProjectileComponent>(entity))
        {
            if (EntityManager.TryGetComponent<ShipEventTeamMarkerComponent>(
                    Transform(args.OtherEntity).GridUid, out var marker))
            {
                if (marker.Team == null || marker.Team == component.Team)
                    return;

                component.Team.Points += (int) Math.Ceiling(GetProjectileDamage(entity) * PointsPerHitMultiplier);
                if (!marker.Team.Hits.Keys.Contains(component.Team))
                    marker.Team.Hits[component.Team] = 0;

                marker.Team.Hits[component.Team]++;
            }
        }
    }

    /// <summary>
    ///     Adds points to other teams for destroying specified one.
    /// </summary>
    /// <param name="team">Team which is destroyed</param>
    private void AddKillPoints(ShipEventTeam team)
    {
        var totalHits = team.Hits.Sum(obj => obj.Value);

        foreach (var (killerTeam, hits) in team.Hits)
        {
            double ratio = hits / (double) totalHits;
            switch (ratio)
            {
                case >= 0.5:
                    killerTeam.Kills++;
                    killerTeam.Points += PointsPerKill;
                    break;
                case >= 0.25:
                    killerTeam.Assists++;
                    killerTeam.Points += PointsPerAssist;
                    break;
            }
        }
    }

    #endregion

    #region Other

    public void SetMarkers(ShipEventTeam team)
    {
        foreach (EntityUid gridUid in team.ShipGrids)
        {
            EnsureComp<ShipEventTeamMarkerComponent>(gridUid).Team = team;

            HashSet<EntityUid> uids = GetGridCompHolders<ShipEventSpawnerComponent>(gridUid);
            uids.UnionWith(GetGridCompHolders<CannonComponent>(gridUid));
            uids.UnionWith(GetGridCompHolders<ShipEventSpawnerComponent>(gridUid));

            foreach (var uid in uids)
            {
                var marker = EntityManager.EnsureComponent<ShipEventTeamMarkerComponent>(uid);
                marker.Team = team;
            }
        }
    }

    private void SetupHUD(ICommonSession session, ShipEventTeam team)
    {
        if (session.AttachedEntity == null)
            return;

        EntityUid uid = session.AttachedEntity.Value;

        if (EntityManager.TryGetComponent<MobHUDComponent>(uid, out var hud))
        {
            var hudProt = _protMan.Index<MobHUDPrototype>(session.Channel.UserName == team.Captain ? CaptainHUDPrototypeId : HUDPrototypeId).ShallowCopy();
            hudProt.Color = team.Color;
            _hudSys.SetActiveHUDs(uid, hud, new List<MobHUDPrototype> { hudProt });
        }
    }

    public void AssignCaptain(ShipEventTeam team, ICommonSession? captain)
    {
        if (captain?.Channel.UserName != team.Captain)
            TeamMessage(team, Loc.GetString("shipevent-team-captainchange", ("oldcap", team.Captain ?? "NONE"), ("newcap", captain?.Channel.UserName ?? "NONE")));

        if (team.Captain != null &&
            _playerMan.TryGetUserId(team.Captain, out var id) &&
            _mindSys.TryGetMind(id, out var mindUid))
        {
            QueueDel(mindUid);
        }

        team.Captain = captain?.Channel.UserName;
        if (captain?.AttachedEntity != null)
        {
            SetupActions(captain, team);
            SetupHUD(captain, team);
        }
    }

    private void OnTeammateSpeak(EntityUid uid, ShipEventTeamMarkerComponent marker, EntitySpokeEvent args)
    {
        if (!RuleSelected)
            return;

        if (args.Channel == null || marker.Team == null)
            return;

        string chatMsg = Loc.GetString("shipevent-team-msg-base", ("name", GetName(args.Source)), ("message", args.Message));

        if (args.Channel.ID == TeamChannelID)
            TeamMessage(marker.Team, chatMsg);

        if (marker.Team.Fleet != null && args.Channel.ID == FleetChannelID)
            FleetMessage(marker.Team.Fleet, chatMsg);

        args.Channel = null;
    }

    private void OnShipNameChange(ShuttleConsoleChangeShipNameMessage args)
    {
        if (!RuleSelected)
            return;

        var gridUid = Transform(GetEntity(args.Entity)).GridUid;
        if (gridUid == null || !TryComp<ShipEventTeamMarkerComponent>(gridUid, out var marker) || marker.Team == null)
            return;

        marker.Team.ShipName = args.NewShipName;
    }

    private void OnSpawnerDestroyed(EntityUid uid, ShipEventSpawnerComponent spawner, ComponentShutdown args)
    {
        if (TryComp<ShipEventTeamMarkerComponent>(uid, out var marker) && marker.Team != null)
            TeamMessage(marker.Team, Loc.GetString("shipevent-spawner-destroyed"), color: Color.DarkRed);
    }

    private void OnPointStorageTriggered(EntityUid uid, ShipEventPointStorageComponent storage, UseInHandEvent args)
    {
        if (EntityManager.TryGetComponent<ShipEventTeamMarkerComponent>(args.User, out var marker))
        {
            if (marker.Team != null)
            {
                TeamMessage(marker.Team, Loc.GetString("shipevent-pointsadded", ("points", storage.Points)));
                marker.Team.Points += storage.Points;
            }
        }
    }

    #endregion
}
