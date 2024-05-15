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

    public string HUDPrototypeId = "ShipeventHUD";
    public string CaptainHUDPrototypeId = "";

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
    public bool RemoveEmptyTeams = true;

    public Action? OnRuleSelected;
    public Action<RoundEndTextAppendEvent>? RoundEndEvent;

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
        SubscribeLocalEvent<ShipEventTeamMarkerComponent, EntitySpokeEvent>(OnTeammateSpeak);

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
        if (!RuleSelected || !Teams.Any())
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
        if (!RuleSelected || !Teams.Any())
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

    private void SetupActions(EntityUid uid, ICommonSession session, ShipEventTeam team)
    {
        _actSys.AddAction(uid, "ShipEventTeamViewToggle");

        if (session.Name == team.Captain)
            _actSys.AddAction(uid, "ShipEventCaptainMenuToggle");

        if (HasComp<GhostComponent>(uid))
            _actSys.AddAction(uid, "ShipEventReturnToLobbyAction");
    }

    private void OnViewToggle(EntityUid uid, ShipEventTeamMarkerComponent marker, ShipEventTeamViewToggleEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        if (!_playerMan.TryGetSessionByEntity(uid, out var session))
            return;

        args.Handled = true;

        List<ShipTeamForTeamViewState> teamsInfo = new();
        foreach (var team in Teams)
        {
            teamsInfo.Add(new ShipTeamForTeamViewState
            {
                Name = team.Name,
                Color = team.Color,
                ShipName = team.ShouldRespawn ? null : team.ShipName,
                AliveCrewCount = team.ShouldRespawn ? null : GetTeamLivingMembers(team).Count().ToString(),
                Points = team.Points,
            });
        }

        Enum uiKey = TeamViewUiKey.Key;
        if (_uiSys.IsUiOpen(uid, uiKey))
            return;
        if (_uiSys.TryGetUi(uid, uiKey, out var bui))
        {
            _uiSys.OpenUi(bui, session);
            _uiSys.SetUiState(bui, new TeamViewBoundUserInterfaceState(teamsInfo));
        }
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
            if (team.Captain != session.Name)
                continue;

            _uiSys.TrySetUiState(uid, uiKey, new ShipEventCaptainMenuBoundUserInterfaceState(
                    team.Members,
                    team.ChosenShipType,
                    team.JoinPassword,
                    team.MaxMembers));
            break;
        }

        _uiSys.TryOpen(uid, uiKey, session);
    }

    private void OnShipPickerInfoRequest(GetShipPickerInfoMessage msg)
    {
        if (!RuleSelected)
            return;

        _uiSys.TrySetUiState(GetEntity(msg.Entity),
            msg.UiKey,
            new ShipPickerBoundUserInterfaceState(ShipTypes));
    }

    private void OnReturnToLobbyAction(ShipEventReturnToLobbyEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        if (!_playerMan.TryGetSessionByEntity(args.Performer, out var session))
            return;

        if (!_uiSys.TryGetUi(args.Performer, GenericWarningUiKey.ShipEventKey, out var bui))
            return;
        _uiSys.SetUiState(bui, new GenericWarningBoundUserInterfaceState
        {
            WarningLoc = "generic-warning-window-warning-to-lobby",
        });
        _uiSys.OpenUi(bui, session);
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
                if (!TryComp<MobStateComponent>(session.AttachedEntity, out var state) || state.CurrentState == MobState.Dead)
                {
                    if (!TrySpawnPlayer(session, team, out _) && HasComp<GhostComponent>(session.AttachedEntity))
                    {
                        if (TryComp<ShipEventTeamMarkerComponent>(session.AttachedEntity, out var marker) && marker.Team == null)
                        {
                            marker.Team = team;
                            SetupActions(session.AttachedEntity.Value, session, team);
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

    //todo: our generic y/n dialog window is very weird
    private void OnReturnPlayerToLobby(GenericWarningYesPressedMessage args)
    {
        if (Equals(args.UiKey, GenericWarningUiKey.ShipEventKey))
        {
            if (TryComp<ShipEventTeamMarkerComponent>(args.Session.AttachedEntity, out var marker) && marker.Team != null)
            {
                marker.Team.Captain = marker.Team.Captain == args.Session.Name ? null : marker.Team.Captain;
                marker.Team.Members.Remove(args.Session.Name);
            }
            _ticker.Respawn(args.Session);
        }
    }

    private bool TrySpawnPlayer(ICommonSession session, ShipEventTeam team, [NotNullWhen(true)] out EntityUid? uid, EntityUid? spawnerUid = null)
    {
        uid = null;

        if (!team.ShipGrids.Any())
            return false;

        spawnerUid ??= GetGridCompHolders<ShipEventSpawnerComponent>(team.ShipGrids).FirstOrNull();
        if (spawnerUid == null)
            return false;

        uid = Spawn(Comp<ShipEventSpawnerComponent>(spawnerUid.Value).Prototype, Transform(spawnerUid.Value).Coordinates);
        Comp<ShipEventTeamMarkerComponent>(uid.Value).Team = team;
        if (!team.Members.Contains(session.Name))
        {
            team.Members.Add(session.Name);
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

        //team HUD setup
        if (EntityManager.TryGetComponent<MobHUDComponent>(uid, out var hud))
        {
            var hudProt = _protMan.Index<MobHUDPrototype>(session.Name == team.Captain ? CaptainHUDPrototypeId : HUDPrototypeId).ShallowCopy();
            hudProt.Color = team.Color;
            _hudSys.SetActiveHUDs(hud, new List<MobHUDPrototype> { hudProt });
        }

        SetupActions(uid.Value, session, team);

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

            if (!activeMembers.Any())
            {
                emptyTeams.Add(team);
                continue;
            }

            if (!livingMembers.Any() && !team.ShouldRespawn)
            {
                QueueTeamRespawn(team, Loc.GetString("shipevent-respawn-dead"));
                continue;
            }

            if (!GetGridCompHolders<ShuttleConsoleComponent>(team.ShipGrids).Any() && !team.ShouldRespawn)
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
            if (team.Captain == null || !_playerMan.TryGetSessionByUsername(team.Captain, out _))
            {
                var newCap = activeMembers.First();
                TeamMessage(team, Loc.GetString("shipevent-team-captainchange", ("oldcap", team.Captain ?? "NONE"), ("newcap", newCap.Name)));
                team.Captain = newCap.Name;
            }

            if (team.ShouldRespawn && team.TimeSinceRemoval > RespawnDelay)
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

        if (!RemoveEmptyTeams)
            return;
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
            if (consoles.Any())
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
            TrySpawnPlayer(session, team, out _);

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

        if (!team.ShipGrids.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-ship-destroyed"), session);
            return;
        }

        var spawners = GetGridCompHolders<ShipEventSpawnerComponent>(team.ShipGrids);
        if (!spawners.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-spawner-destroyed"), session);
            return;
        }

        if (TrySpawnPlayer(session, team, out var uid))
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
            team.ShouldRespawn = true;
            team.TimeSinceRemoval = 0;
        }
    }

    private void ImmediateTeamRespawn(ShipEventTeam team)
    {
        SpawnShip(team);
        SetMarkers(team);

        foreach (var session in GetTeamSessions(team))
        {
            TrySpawnPlayer(session, team, out _);
        }

        team.Respawns++;
        team.ShouldRespawn = false;
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

    private void OnTeammateSpeak(EntityUid uid, ShipEventTeamMarkerComponent marker, EntitySpokeEvent args)
    {
        if (!RuleSelected)
            return;

        if (args.Channel == null || marker.Team == null || args.Channel.ID != "Common")
            return;

        string chatMsg = Loc.GetString("shipevent-team-msg-base", ("name", GetName(args.Source)), ("message", args.Message));

        TeamMessage(marker.Team, chatMsg);
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
            TeamMessage(marker.Team, Loc.GetString("shipevent-spawnerdestroyed"), color: Color.DarkRed);
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
