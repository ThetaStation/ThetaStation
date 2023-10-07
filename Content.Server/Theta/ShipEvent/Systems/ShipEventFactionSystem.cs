using System.Linq;
using System.Numerics;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Mind;
using Content.Server.Players;
using Content.Server.Preferences.Managers;
using Content.Server.Radio.Components;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.DebrisGeneration;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Content.Server.Theta.MobHUD;
using Content.Server.Theta.NiceColors;
using Content.Server.Theta.NiceColors.ColorPalettes;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.Projectiles;
using Content.Shared.Roles.Theta;
using Content.Shared.Shuttles.Events;
using Content.Shared.Theta.MobHUD;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Misc.GenericWarningUI;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;

    [Dependency] private readonly ActionsSystem _actSys = default!;

    [Dependency] private readonly ChatSystem _chatSys = default!;

    [Dependency] private readonly IChatManager _chatManager = default!;

    [Dependency] private readonly MobHUDSystem _hudSys = default!;

    [Dependency] private readonly DebrisGenerationSystem _debrisSys = default!;

    [Dependency] private readonly IdentitySystem _idSys = default!;

    [Dependency] private readonly MapLoaderSystem _mapSys = default!;

    [Dependency] private readonly IMapManager _mapMan = default!;

    [Dependency] private readonly IPrototypeManager _protMan = default!;

    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;

    [Dependency] private readonly IPlayerManager _playerMan = default!;

    [Dependency] private readonly TransformSystem _formSys = default!;

    [Dependency] private readonly RoundEndSystem _endSys = default!;

    [Dependency] private readonly MindSystem _mindSystem = default!;

    [Dependency] private readonly RoleSystem _roleSystem = default!;

    [Dependency] private readonly PlayerFactionSystem _factionSystem = default!;

    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;

    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;

    [Dependency] private readonly IGameTiming _timing = default!;

    //used when setting up buttons for ghosts, in cases when mind from shipevent agent is transferred to null and not to ghost entity directly
    private Dictionary<IPlayerSession, ShipEventFaction> lastTeamLookup = new();

    private readonly Dictionary<string, int> _projectileDamage = new(); //cached damage for projectile prototypes
    private int _lastTeamNumber;
    private float _teamCheckTimer;
    public float RoundendTimer;
    private float _boundsCompressionTimer;
    private int _lastAnnoucementMinute;

    //all time-related fields are specified in seconds
    public float RoundDuration;
    public bool TimedRoundEnd = false;

    public float TeamCheckInterval;
    public float RespawnDelay;

    public int MaxSpawnOffset;

    public int BonusInterval;
    public int PointsPerInterval; //points for surviving longer than BonusInterval without respawn

    public float PointsPerHitMultiplier;
    public int PointsPerAssist;
    public int PointsPerKill;
    public int OutOfBoundsPenalty; //deducted every update cycle for teams which left play area

    public string HUDPrototypeId = "ShipeventHUD";
    public string CaptainHUDPrototypeId = "";

    public bool BoundsCompression = false;
    public float BoundsCompressionInterval;
    public int BoundsCompressionDistance; //how much play area bounds are compressed every BoundCompressionInterval
    public int CurrentBoundsOffset; //inward offset of bounds

    public bool RuleSelected;

    public List<ShipTypePrototype> ShipTypes = new();
    public MapId TargetMap;

    public List<ShipEventFaction> Teams { get; } = new();

    public List<Processor> ShipProcessors = new();

    public ColorPalette ColorPalette = new ShipEventPalette();
    
    //used by flag capture
    public bool AllowTeamRegistration = true;
    public bool RemoveEmptyTeams = true;
    public Action<RoundEndTextAppendEvent>? RoundEndEvent;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShipEventFactionMarkerComponent, ShipEventTeamViewToggleEvent>(OnViewToggle);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, ShipEventCaptainMenuToggleEvent>(OnCapMenuToggle);

        SubscribeLocalEvent<ShipEventReturnToLobbyEvent>(OnReturnToLobbyAction);
        SubscribeLocalEvent<GenericWarningYesPressedMessage>(ReturnToLobbyPlayer);

        SubscribeLocalEvent<ShipEventFactionMarkerComponent, StartCollideEvent>(OnCollision);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, MindTransferredMessage>(OnPlayerTransfer);
        SubscribeLocalEvent<GhostAttemptHandleEvent>(OnPlayerGhostAttempt);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, EntitySpokeEvent>(OnTeammateSpeak);

        SubscribeLocalEvent<ShipEventPointStorageComponent, UseInHandEvent>(OnPointStorageTriggered);

        SubscribeAllEvent<ShuttleConsoleChangeShipNameMessage>(OnShipNameChange); //un-directed event since we will have duplicate subscriptions otherwise
        SubscribeAllEvent<GetShipPickerInfoMessage>(OnShipPickerInfoRequest);
        SubscribeAllEvent<BoundsOverlayInfoRequest>(OnBoundsOverlayInfoRequest);

        InitializeCaptainMenu();
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundEndDiscordTextAppendEvent>(OnRoundEndDiscord);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    //for handling intentional ghosting (suicide)
    private void OnPlayerGhostAttempt(GhostAttemptHandleEvent args)
    {
        AfterSpawn(SpawnPlayer(args.MindUid));
        args.Handled = true;
        args.Result = true;
    }

    private void OnTeammateSpeak(EntityUid uid, ShipEventFactionMarkerComponent marker, EntitySpokeEvent args)
    {
        if (args.Channel == null)
            return;
        if (!EntityManager.HasComponent<WearingHeadsetComponent>(uid) && args.Channel.ID != "Common")
            return;
        if (marker.Team == null)
            return;

        if(!_mindSystem.TryGetMind(args.Source, out _, out var mind))
            return;
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;

        var chatMsg = Loc.GetString("shipevent-team-msg-base", ("name", session.ConnectedClient.UserName), ("message", args.Message));

        TeamMessage(marker.Team, chatMsg);
        args.Channel = null;
    }

    public override void Update(float frametime)
    {
        if (!RuleSelected)
            return;

        _teamCheckTimer += frametime;
        RoundendTimer += frametime;
        _boundsCompressionTimer += frametime;

        if (_teamCheckTimer > TeamCheckInterval)
        {
            _teamCheckTimer -= TeamCheckInterval;
            CheckTeams(TeamCheckInterval);
        }

        CheckBoundsCompressionTimer();
        CheckRoundendTimer();
        CheckPickupsTimer();
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

    private void OnShipPickerInfoRequest(GetShipPickerInfoMessage msg)
    {
        _uiSys.TrySetUiState(GetEntity(msg.Entity),
            msg.UiKey,
            new ShipPickerBoundUserInterfaceState(ShipTypes));
    }

    private void OnShipNameChange(ShuttleConsoleChangeShipNameMessage args)
    {
        var shipGrid = Transform(GetEntity(args.Entity)).GridUid;
        if (shipGrid == null)
            return;

        foreach (var team in Teams)
        {
            if (team.Ship == shipGrid)
            {
                var newName = args.NewShipName;
                team.ShipName = newName;
                break;
            }
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _lastTeamNumber = 0;
        _teamCheckTimer = 0;
        RoundendTimer = 0;
        _boundsCompressionTimer = 0;
        _lastAnnoucementMinute = 0;

        CurrentBoundsOffset = 0;

        RuleSelected = false;

        ShipTypes.Clear();
        TargetMap = MapId.Nullspace;

        Teams.Clear();
        lastTeamLookup.Clear();

        ShipProcessors.Clear();

        PickupPositions.Clear();
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
                ("capname", team.Captain)
            ));
            args.AddLine(Loc.GetString("shipevent-roundend-teamstats",
                ("points", team.Points),
                ("kills", team.Kills),
                ("assists", team.Assists),
                ("respawns", team.Respawns)
            ));
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

        args.AddLine(Loc.GetString("shipevent-roundend-discord-team",
            ("capname", winner.Captain)
        ));
        args.AddLine(Loc.GetString("shipevent-roundend-discord-teamstats",
            ("points", winner.Points),
            ("kills", winner.Kills),
            ("assists", winner.Assists),
            ("respawns", winner.Respawns)
        ));
    }

    private void OnViewToggle(EntityUid entity, ShipEventFactionMarkerComponent marker,
        ShipEventTeamViewToggleEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        if(!_mindSystem.TryGetMind(entity, out _, out var mind))
            return;
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;

        args.Handled = true;

        List<ShipTeamForTeamViewState> teamsInfo = new();
        foreach (var team in Teams)
        {
            var memberCountStr = _factionSystem.GetLivingMembersMinds(team).Count.ToString();
            teamsInfo.Add(new ShipTeamForTeamViewState
            {
                Name = team.Name,
                Color = team.Color,
                ShipName = team.ShouldRespawn ? null : team.ShipName,
                AliveCrewCount = team.ShouldRespawn ? null : memberCountStr,
                Points = team.Points,
            });
        }

        Enum uiKey = TeamViewUiKey.Key;
        if (_uiSys.IsUiOpen(entity, uiKey))
            return;
        if (_uiSys.TryGetUi(entity, uiKey, out var bui))
        {
            _uiSys.OpenUi(bui, session);
            _uiSys.SetUiState(bui, new TeamViewBoundUserInterfaceState(teamsInfo));
        }
    }

    private void OnCapMenuToggle(EntityUid uid, ShipEventFactionMarkerComponent marker, ShipEventCaptainMenuToggleEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        if(!_mindSystem.TryGetMind(uid, out _, out var mind))
            return;
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;

        args.Handled = true;

        Enum uiKey = CaptainMenuUiKey.Key;

        if (_uiSys.IsUiOpen(uid, uiKey))
            return;

        foreach (var team in Teams)
        {
            if (team.Captain != session.ConnectedClient.UserName)
                continue;
            var members = _factionSystem.GetMemberUserNames(team);
            _uiSys.TrySetUiState(uid,
                uiKey,
                new ShipEventCaptainMenuBoundUserInterfaceState(members, team.ChosenShipType, team.JoinPassword, team.MaxMembers)
                );
            break;
        }

        _uiSys.TryOpen(uid, uiKey, session);
    }

    private void OnReturnToLobbyAction(ShipEventReturnToLobbyEvent args)
    {
        if(!_playerMan.TryGetSessionByEntity(args.Performer, out var session))
            return;

        if(!_uiSys.TryGetUi(args.Performer, GenericWarningUiKey.ShipEventKey, out var bui))
            return;
        _uiSys.SetUiState(bui, new GenericWarningBoundUserInterfaceState
            {
                WarningLoc = "generic-warning-window-warning-to-lobby",
            }
        );
        _uiSys.OpenUi(bui, (IPlayerSession) session);
    }

    private void ReturnToLobbyPlayer(GenericWarningYesPressedMessage args)
    {
        if(Equals(args.UiKey, GenericWarningUiKey.ShipEventKey))
            _ticker.Respawn((IPlayerSession) args.Session);
    }

    //for handling deaths
    private void OnPlayerTransfer(EntityUid uid, ShipEventFactionMarkerComponent marker, MindTransferredMessage args)
    {
        //valid entity -> null case
        if (args.NewEntity == null)
        {
            if (args.Mind.Session == null || marker.Team == null)
                return;

            lastTeamLookup[(IPlayerSession)args.Mind.Session] = marker.Team;
            return;
        }
        
        //null -> valid entity case
        if (args.NewEntity == uid)
        {
            if (!_mindSystem.TryGetSession(args.Mind, out var session))
                return;

            if (lastTeamLookup.TryGetValue(session, out var team))
            {
                marker.Team = team;
                lastTeamLookup.Remove(session);
            }
        }

        //entity is actually ghost
        if (HasComp<GhostComponent>(args.NewEntity))
        {
            if (marker.Team == null)
                return;
            
            if (marker.Team.ShouldRespawn)
                return;
            
            AfterSpawn(SpawnPlayer(args.MindUid));
        }
    }

    private void OnCollision(EntityUid entity, ShipEventFactionMarkerComponent component, ref StartCollideEvent args)
    {
        if (component.Team == null)
            return;

        if (EntityManager.HasComponent<ProjectileComponent>(entity))
        {
            if (EntityManager.TryGetComponent<ShipEventFactionMarkerComponent>(
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
    /// Does everything needed to create a new team, from faction creation to ship spawning.
    /// </summary>
    public void CreateTeam(ICommonSession session, string name, ShipTypePrototype? initialShipType, 
        string? password, int maxMembers, bool noCaptain = false)
    {
        if (!RuleSelected || !AllowTeamRegistration)
            return;

        ShipTypePrototype shipType = initialShipType ?? _random.Pick(ShipTypes.Where(t => t.MinCrewAmount == 1).ToList());

        var ship = _debrisSys.RandomPosSpawn(
            TargetMap,
            new Vector2(CurrentBoundsOffset, CurrentBoundsOffset),
            MaxSpawnOffset - CurrentBoundsOffset,
            50,
            _protMan.Index<StructurePrototype>(shipType.StructurePrototype),
            ShipProcessors,
            true);

        var color = ColorPalette.GetNextColor();
        var team = RegisterTeam(noCaptain ? "N/A" : session.ConnectedClient.UserName, name, color);
        team.ChosenShipType = shipType;
        team.Ship = ship;
        team.ShipName = name;
        team.JoinPassword = password;
        team.MaxMembers = maxMembers;

        SetMarkers(ship, team);
        SetShipName(ship, name);

        if (noCaptain)
            return;
        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>(ship);
        if (!spawners.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-respawnfailed"), (IPlayerSession)session);
            return;
        }
        
        AfterSpawn(SpawnPlayer((IPlayerSession)session, spawners.First()));
    }

    /// <summary>
    /// Adds player to faction (by name), spawns him on ship & does all other necessary stuff
    /// </summary>
    /// <param name="player">player's session</param>
    /// <param name="team">team's faction</param>
    /// <param name="password">password of the team</param>
    public void JoinTeam(IPlayerSession player, ShipEventFaction team, string? password)
    {
        if (team.JoinPassword != password)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-incorrect-password"), player);
            return;
        }

        if (team.MaxMembers != 0 && team.Members.Count >= team.MaxMembers)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-memberlimit"), player);
            return;
        }

        if (team.Ship == EntityUid.Invalid)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-ship-destroyed"), player);
            return;
        }

        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>(team.Ship);
        if (!spawners.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-spawner-destroyed"), player);
            return;
        }
        
        var spawner = spawners.First();
        var playerMob = SpawnPlayer(player, spawner);
        AfterSpawn(playerMob);

        TeamMessage(team, Loc.GetString("shipevent-team-newmember", ("name", GetName(playerMob))));
    }

    /// <summary>
    /// Spawns player using specified session & spawner
    /// </summary>
    /// <param name="player">player's session</param>
    /// <param name="spawnerUid">spawner's entity</param>
    /// <returns>player's entity</returns>
    private EntityUid SpawnPlayer(IPlayerSession player, EntityUid spawnerUid)
    {
        var spawner = EntityManager.GetComponent<ShipEventSpawnerComponent>(spawnerUid);
        var playerMob = EntityManager.SpawnEntity(spawner.Prototype, Transform(spawnerUid).Coordinates);
        var xform = EntityManager.GetComponent<TransformComponent>(playerMob);
        xform.AttachToGridOrMap();

        if (EntityManager.TryGetComponent<ShipEventFactionMarkerComponent>(spawnerUid, out var spawnerMarker) &&
            spawnerMarker.Team != null)
        {
            var playerMarker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(playerMob);
            playerMarker.Team = spawnerMarker.Team;
        }

        EntityUid mind;
        if (player.AttachedEntity != null && _mindSystem.TryGetMind(player.AttachedEntity.Value, out var mindId, out _))
            mind = mindId;
        else
            mind = _mindSystem.CreateMind(player.UserId, Name(playerMob));

        var lastEntity = player.AttachedEntity;
        _mindSystem.TransferTo(mind, playerMob);

        if(lastEntity != null)
            EntityManager.QueueDeleteEntity(lastEntity.Value);

        HumanoidCharacterProfile profile;
        if (_prefsManager.TryGetCachedPreferences(player.UserId, out var preferences))
            profile = (HumanoidCharacterProfile) preferences.GetProfile(preferences.SelectedCharacterIndex);
        else
            profile = HumanoidCharacterProfile.Random();

        _humanoidAppearanceSystem.LoadProfile(playerMob, profile);

        return playerMob;
    }

    /// <summary>
    /// Spawns player using specified mind
    /// </summary>
    /// <param name="uid">Mind entity</param>
    /// <returns></returns>
    private EntityUid SpawnPlayer(EntityUid uid)
    {
        if(!TryComp<MindComponent>(uid, out var mindComponent))
            return EntityUid.Invalid;
        if(mindComponent.CurrentEntity == null ||
           !TryComp<ShipEventFactionMarkerComponent>(mindComponent.CurrentEntity, out var marker))
            return EntityUid.Invalid;

        var ship = marker.Team?.Ship;
        if (ship == null)
            return EntityUid.Invalid;

        if (!_mindSystem.TryGetSession(uid, out var session))
            return EntityUid.Invalid;
        
        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>(ship.Value);
        if (!spawners.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-respawnfailed"), session);
            return EntityUid.Invalid;
        }

        var spawner = spawners.First();
        return SpawnPlayer(session, spawner);
    }
    
    /// <summary>
    /// Spawns player using specified session
    /// </summary>
    /// <param name="session">Player session</param>
    /// <returns></returns>
    private EntityUid SpawnPlayer(IPlayerSession session)
    {
        var mind = session.GetMind();
        if(mind == null)
            return EntityUid.Invalid;
        return SpawnPlayer(mind.Value);
    }

    /// <summary>
    /// Sets up roles, HUD, action buttons, team marker & other stuff after spawn
    /// </summary>
    private void AfterSpawn(EntityUid spawnedEntity)
    {
        if (!spawnedEntity.IsValid())
            return;

        if(!_mindSystem.TryGetMind(spawnedEntity, out var mindId, out _))
            return;
        if(!_mindSystem.TryGetSession(mindId, out var session))
            return;

        var team = Comp<ShipEventFactionMarkerComponent>(Transform(spawnedEntity).GridUid!.Value).Team;
        if (team == null)
            return;

        if(!_roleSystem.MindHasRole<ShipEventRoleComponent>(mindId))
        {
            var shipEventRoleComponent = new ShipEventRoleComponent { PrototypeId = "ShipTester" };
            _roleSystem.MindAddRole(mindId, shipEventRoleComponent);
            team.AddMember(shipEventRoleComponent);
        }

        SetPlayerCharacterName(spawnedEntity, $"{GetName(spawnedEntity)} ({team.Name})");

        if (EntityManager.TryGetComponent<MobHUDComponent>(spawnedEntity, out var hud))
        {
            var hudProt = _protMan.Index<MobHUDPrototype>(
                session.ConnectedClient.UserName == team.Captain ? CaptainHUDPrototypeId : HUDPrototypeId).ShallowCopy();
            hudProt.Color = team.Color;
            _hudSys.SetActiveHUDs(hud, new List<MobHUDPrototype> { hudProt });
        }
        _chatManager.DispatchServerMessage(session, Loc.GetString("shipevent-role-greet"));
    }

    private void OnMindAdded(EntityUid uid, ShipEventFactionMarkerComponent component, MindAddedMessage args)
    {
        if(component.Team == null)
            return;
        if (!_mindSystem.TryGetMind(uid, out _, out var mind))
            return;
        if (!_mindSystem.TryGetSession(mind, out var session))
            return;
        SetupActions(uid, component.Team, session);
    }

    /// <summary>
    /// Sets up action buttons for specified player
    /// </summary>
    /// <param name="uid">player's uid</param>
    /// <param name="team">player's team</param>
    /// <param name="session">player's session</param>
    private void SetupActions(EntityUid uid, ShipEventFaction team, IPlayerSession session)
    {
        _actSys.AddAction(uid,"ShipEventTeamViewToggle");
        if (team.Captain == session.ConnectedClient.UserName)
        {
            _actSys.AddAction(uid, "ShipEventCaptainMenuToggle");
        }
    }

    /// <summary>
    /// Creates new faction with all the specified data. Does not spawn ship, if you want to put new team in game right away use CreateTeam
    /// </summary>
    private ShipEventFaction RegisterTeam(string captain, string name, Color color)
    {
        var teamName = IsValidName(name) ? name : GenerateTeamName();
        var teamColor = IsValidColor(color) ? color : GenerateTeamColor();

        var team = new ShipEventFaction(
            teamName,
            "",
            teamColor,
            captain);

        Teams.Add(team);

        return team;
    }

    /// <summary>
    ///     Sets ShipEventFactionMarker components teams on spawner, cannons, etc.
    /// </summary>
    /// <param name="shipEntity">Ship in question</param>
    /// <param name="team">Team in question</param>
    public void SetMarkers(EntityUid shipEntity, ShipEventFaction team)
    {
        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>(shipEntity);
        foreach (var spawner in spawners)
        {
            var marker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(spawner);
            marker.Team = team;
        }

        var cannons = GetShipComponentHolders<CannonComponent>(shipEntity);
        foreach (var cannon in cannons)
        {
            var marker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(cannon);
            marker.Team = team;
        }

        var markerShip = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(shipEntity);
        markerShip.Team = team;
    }

    /// <summary>
    ///     Deletes current team ship & members, and marks it for respawn
    /// </summary>
    /// <param name="team">Team to respawn</param>
    /// <param name="respawnReason">Message to show in team message</param>
    /// <param name="immediate">If this team should be respawned without delay</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on respawned one</param>
    private void RespawnTeam(ShipEventFaction team, string respawnReason = "", bool immediate = false, bool killPoints = true)
    {
        var message = Loc.GetString(
                "shipevent-team-respawn",
                ("respawnreason", respawnReason == "" ? Loc.GetString("shipevent-respawn-default") : respawnReason),
                ("respawntime", RespawnDelay / 60));
            TeamMessage(team, message);

        if (killPoints)
            AddKillPoints(team);

        team.Hits.Clear();

        foreach (var member in team.Members)
        {
            if(TryComp<MindComponent>(member.Owner, out var mind) && mind.CurrentEntity != null)
                EntityManager.QueueDeleteEntity(mind.CurrentEntity.Value);
        }

        DetachEnemyTeamsFromGrid(team.Ship, team);

        EntityManager.DeleteEntity(team.Ship);

        team.Ship = EntityUid.Invalid;

        if (immediate)
            ImmediateRespawn(team);
        else
            team.ShouldRespawn = true;
    }

    /// <summary>
    ///     Immediately respawns team
    /// </summary>
    /// <param name="team">Team to respawn</param>
    /// <param name="oldShipName">
    ///     Name of previous ship, so new one can have it too. New name will be empty if this is not
    ///     specified
    /// </param>
    private void ImmediateRespawn(ShipEventFaction team)
    {
        StructurePrototype shipStructProt;
        if (team.ChosenShipType?.StructurePrototype != null)
        {
            shipStructProt = _protMan.Index<StructurePrototype>(team.ChosenShipType.StructurePrototype);
        }
        else
        {
            shipStructProt = _protMan.Index<StructurePrototype>(_random.Pick(ShipTypes).StructurePrototype);
        }

        var newShip = _debrisSys.RandomPosSpawn(
            TargetMap,
            new Vector2(CurrentBoundsOffset, CurrentBoundsOffset),
            MaxSpawnOffset - CurrentBoundsOffset,
            50,
            shipStructProt,
            ShipProcessors,
            true);

        SetMarkers(newShip, team);
        SetShipName(newShip, team.ShipName);

        team.Ship = newShip;

        List<IPlayerSession> sessions = new();
        foreach (var member in team.Members)
        {
            if (!_mindSystem.TryGetSession(member.Owner, out var session))
                continue;
            sessions.Add(session);
        }
        
        foreach (var session in sessions)
        {
            AfterSpawn(SpawnPlayer(session));
        }

        team.Respawns++;
        team.ShouldRespawn = false;
    }

    /// <summary>
    /// Removes team entirely
    /// </summary>
    /// <param name="team">Team to remove</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on removed one</param>
    public void RemoveTeam(ShipEventFaction team, string removalReason = "", bool killPoints = true)
    {
        var message = Loc.GetString(
            "shipevent-team-remove",
            ("removereason", removalReason == "" ? Loc.GetString("shipevent-remove-default") : removalReason));
        TeamMessage(team, message);
        
        if (killPoints)
            AddKillPoints(team);

        foreach (var member in team.Members)
        {
            if(TryComp<MindComponent>(member.Owner, out var mind) && mind.CurrentEntity != null)
                EntityManager.QueueDeleteEntity(mind.CurrentEntity.Value);
        }

        if (team.Ship != EntityUid.Invalid)
            EntityManager.QueueDeleteEntity(team.Ship);

        Teams.Remove(team);
    }

    /// <summary>
    ///     Adds points to other teams for destroying specified one.
    /// </summary>
    /// <param name="team">Team which is destroyed</param>
    private void AddKillPoints(ShipEventFaction team)
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

    private void CheckTeams(float deltaTime)
    {
        List<ShipEventFaction> emptyTeams = new();
        foreach (var team in Teams)
        {
            if(_factionSystem.GetActiveMembers(team).Count == 0)
            {
                emptyTeams.Add(team);
                continue;
            }

            if (!_factionSystem.GetLivingMembersMinds(team).Any() && team.Members.Any() && !team.ShouldRespawn)
            {
                RespawnTeam(
                    team,
                    Loc.GetString("shipevent-respawn-dead"));
                team.TimeSinceRemoval = 0;
                continue;
            }

            if (!GetShipComponentHolders<ShuttleConsoleComponent>(team.Ship).Any() && !team.ShouldRespawn)
            {
                RespawnTeam(
                    team,
                    Loc.GetString("shipevent-respawn-tech"));
                team.TimeSinceRemoval = 0;
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

            if (!_playerMan.TryGetSessionByUsername(team.Captain, out _))
            {
                if (team.Members.Any())
                {
                    var activeMembers = _factionSystem.GetActiveMembers(team);
                    _mindSystem.TryGetSession(activeMembers[0].Owner, out var session);
                    var newCap = activeMembers.Count > 0 ? session!.ConnectedClient.UserName : "N/A";
                    TeamMessage(team, Loc.GetString("shipevent-team-captainchange", ("oldcap", team.Captain), ("newcap", newCap)));
                    team.Captain = newCap;
                }
            }

            if (team.ShouldRespawn && team.TimeSinceRemoval > RespawnDelay)
            {
                TeamMessage(team, Loc.GetString("shipevent-team-respawnnow"));
                ImmediateRespawn(team);
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

            team.TimeSinceRemoval += deltaTime;
        }

        if (!RemoveEmptyTeams)
            return;
        foreach (var team in emptyTeams)
        {
            RemoveTeam(team, Loc.GetString("shipevent-remove-noplayers"));
        }
    }

    private void OnPointStorageTriggered(EntityUid uid, ShipEventPointStorageComponent storage, UseInHandEvent args)
    {
        if (EntityManager.TryGetComponent<ShipEventFactionMarkerComponent>(args.User, out var marker))
        {
            if (marker.Team != null)
            {
                TeamMessage(marker.Team, Loc.GetString("shipevent-pointsadded", ("points", storage.Points)));
                marker.Team.Points += storage.Points;
            }
        }
    }
}
