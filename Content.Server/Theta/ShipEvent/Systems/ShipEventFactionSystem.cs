using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Corvax.RoundNotifications;
using Content.Server.GameTicking;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.DebrisGeneration;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Content.Server.Theta.MobHUD;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Preferences;
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.Events;
using Content.Shared.Theta.MobHUD;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;


namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actSys = default!;
    [Dependency] private readonly ChatSystem _chatSys = default!;
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
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearanceSystem = default!;
    [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;

    private readonly Dictionary<string, int> _projectileDamage = new(); //cached damage for projectile prototypes
    private int _lastTeamNumber;
    private float _teamCheckTimer;
    public float RoundendTimer;
    private float _boundsCompressionTimer;
    private float _lootboxTimer;
    private int _lastAnnoucementMinute;

    //all time-related fields are specified in seconds
    public float RoundDuration;
    public bool TimedRoundEnd = false;

    public float TeamCheckInterval;
    public float RespawnDelay;

    public float LootboxSpawnInterval;
    public int LootboxSpawnAmount;
    public float LootboxLifetime;
    public List<StructurePrototype> LootboxPrototypes = new();

    public int MaxSpawnOffset;

    public int BonusInterval;
    public int PointsPerInterval; //points for surviving longer than BonusInterval without respawn

    public float PointsPerHitMultiplier;
    public int PointsPerAssist;
    public int PointsPerKill;
    public int OutOfBoundsPenalty; //deducted every update cycle for teams which left play area

    public int PlayersPerTeamPlace;

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
    public List<Processor> LootboxProcessors = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShipEventFactionViewComponent, ShipEventTeamViewToggleEvent>(OnViewToggle);
        SubscribeLocalEvent<ShipEventCaptainMenuComponent, ShipEventCaptainMenuToggleEvent>(OnCapMenuToggle);

        SubscribeLocalEvent<ShipEventFactionMarkerComponent, StartCollideEvent>(OnCollision);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, MobStateChangedEvent>(OnPlayerStateChange);

        SubscribeLocalEvent<ShipEventLootboxSpawnTriggerComponent, UseInHandEvent>(OnLootboxSpawnTriggered);
        SubscribeLocalEvent<ShipEventPointStorageComponent, UseInHandEvent>(OnPointStorageTriggered);

        SubscribeAllEvent<ShuttleConsoleChangeShipNameMessage>(OnShipNameChange); //un-directed event since we will have duplicate subscriptions otherwise
        SubscribeAllEvent<GetShipPickerInfoMessage>(OnShipPickerInfoRequest);
        SubscribeAllEvent<BoundsOverlayInfoRequest>(OnBoundsOverlayInfoRequest);
        SubscribeAllEvent<LootboxInfoRequest>(OnLootboxInfoRequest);

        SubscribeAllEvent<ShipEventCaptainMenuRequestInfoMessage>(OnCapMenuInfoRequest);
        SubscribeAllEvent<ShipEventCaptainMenuChangeShipMessage>(OnShipChangeRequest);
        SubscribeAllEvent<ShipEventCaptainMenuChangeBlacklistMessage>(OnBlacklistChangeRequest);
        SubscribeAllEvent<ShipEventCaptainMenuKickMemberMessage>(OnKickMemberRequest);

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundEndDiscordTextAppendEvent>(OnRoundEndDiscord);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frametime)
    {
        if (!RuleSelected)
            return;

        _teamCheckTimer += frametime;
        RoundendTimer += frametime;
        _boundsCompressionTimer += frametime;
        _lootboxTimer += frametime;

        if (_teamCheckTimer > TeamCheckInterval)
        {
            _teamCheckTimer -= TeamCheckInterval;
            CheckTeams(TeamCheckInterval);
        }

        CheckBoundsCompressionTimer();
        CheckLootboxTimer(frametime);
        CheckRoundendTimer();
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

    private void OnCapMenuInfoRequest(ShipEventCaptainMenuRequestInfoMessage msg)
    {
        foreach (var team in Teams)
        {
            if (team.Captain == msg.Session.ConnectedClient.UserName)
            {
                _uiSys.TrySetUiState(msg.Entity,
                    msg.UiKey,
                    new ShipEventCaptainMenuBoundUserInterfaceState(team.GetMemberUserNames(), team.ChosenShipType));
                return;
            }
        }
    }

    private void OnShipPickerInfoRequest(GetShipPickerInfoMessage msg)
    {
        var memberCount = 1;
        foreach (var team in Teams)
        {
            if (team.Captain == msg.Session.ConnectedClient.UserName)
            {
                memberCount = team.Members.Count;
                break;
            }
        }

        _uiSys.TrySetUiState(msg.Entity,
            msg.UiKey,
            new ShipPickerBoundUserInterfaceState(ShipTypes, memberCount));
    }

    private void OnShipNameChange(ShuttleConsoleChangeShipNameMessage args)
    {
        var shipGrid = Transform(args.Entity).GridUid;
        if (shipGrid == null)
            return;

        foreach (var team in Teams)
        {
            if (team.Ship == shipGrid)
            {
                var newName = args.NewShipName;

                var message = Loc.GetString(
                    "shipevent-team-shiprename",
                    ("teamname", team.Name),
                    ("oldname", team.ShipName),
                    ("newname", newName));
                Announce(message);
                team.ShipName = newName;
                break;
            }
        }
    }

    public void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _lastTeamNumber = 0;
        _teamCheckTimer = 0;
        RoundendTimer = 0;
        _boundsCompressionTimer = 0;
        _lootboxTimer = 0;
        _lastAnnoucementMinute = 0;

        LootboxPrototypes.Clear();

        CurrentBoundsOffset = 0;

        ShipTypes.Clear();
        TargetMap = MapId.Nullspace;

        Teams.Clear();

        ShipProcessors.Clear();
        LootboxProcessors.Clear();
    }

    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
        if (!RuleSelected || !Teams.Any())
            return;

        ClearMindTracker();

        var winner = Teams.First();
        args.AddLine(Loc.GetString("shipevent-roundend-heading"));
        foreach (var team in Teams)
        {
            if (team.Points > winner.Points)
                winner = team;

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

    private void OnViewToggle(EntityUid entity, ShipEventFactionViewComponent component,
        ShipEventTeamViewToggleEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        var session = GetSession(entity);
        if (session == null)
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
                AliveCrewCount = team.ShouldRespawn ? null : team.GetLivingMembersMinds().Count.ToString(),
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

    private void OnCapMenuToggle(EntityUid uid, ShipEventCaptainMenuComponent component, ShipEventCaptainMenuToggleEvent args)
    {
        if (!RuleSelected || args.Handled)
            return;

        var session = GetSession(uid);
        if (session == null)
            return;

        args.Handled = true;

        Enum uiKey = CaptainMenuUiKey.Key;
        if (_uiSys.IsUiOpen(uid, uiKey))
            return;
        if (_uiSys.TryGetUi(uid, uiKey, out var bui))
            _uiSys.OpenUi(bui, session);
    }

    private void OnPlayerStateChange(EntityUid entity, ShipEventFactionMarkerComponent marker, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var ship = marker.Team?.Ship;
        if (ship == null)
            return;

        var session = GetSession(entity);
        if (session == null)
            return;

        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>((EntityUid)ship);

        if (!spawners.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-respawnfailed"), session);
            return;
        }

        var spawner = spawners.First();
        var playerMob = SpawnPlayer(session, spawner);
        AfterSpawn(playerMob, spawner);
    }

    private void OnCollision(EntityUid entity, ShipEventFactionMarkerComponent component, ref StartCollideEvent args)
    {
        if (component.Team == null)
            return;

        if (EntityManager.TryGetComponent(entity, out ProjectileComponent? projComp))
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
    public void CreateTeam(ICommonSession captainSession, string name, Color color, ShipTypePrototype? initialShipType = null,
        List<string>? blacklist = null)
    {
        if (!RuleSelected)
            return;

        ShipTypePrototype shipType = initialShipType ?? _random.Pick(ShipTypes.Where(t => t.MinCrewAmount == 1).ToList());

        var newShip = _debrisSys.RandomPosSpawn(
            TargetMap,
            new Vector2(CurrentBoundsOffset, CurrentBoundsOffset),
            MaxSpawnOffset - CurrentBoundsOffset,
            50,
            _protMan.Index<StructurePrototype>(shipType.StructurePrototype),
            ShipProcessors,
            true);

        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>(newShip);
        if (!spawners.Any())
            return;

        var team = RegisterTeam(captainSession.ConnectedClient.UserName, name, color, blacklist);
        team.ChosenShipType = shipType;
        team.Ship = newShip;
        SetMarkers(newShip, team);

        var spawner = spawners.First();
        var playerMob = SpawnPlayer((IPlayerSession) captainSession, spawner);
        AfterSpawn(playerMob, spawner);
    }

    /// <summary>
    /// Adds player to faction (by name), spawns him on ship & does all other necessary stuff
    /// </summary>
    /// <param name="player">player's session</param>
    /// <param name="teamName">name of the team</param>
    public void JoinTeam(IPlayerSession player, string teamName)
    {
        EntityUid? shipUid = null;
        ShipEventFaction targetTeam = default!;

        foreach (var team in Teams)
        {
            if (team.Name == teamName)
            {
                targetTeam = team;
                shipUid = team.Ship;
                break;
            }
        }

        if (targetTeam.Members.Count >= GetMemberLimit())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-memberlimit"), player);
            return;
        }

        if (targetTeam.Blacklist != null)
        {
            if (targetTeam.Blacklist.Contains(player.ConnectedClient.UserName))
            {
                _chatSys.SendSimpleMessage(Loc.GetString("shipevent-blacklist"), player);
                return;
            }
        }

        if (shipUid == null)
            return;

        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>(shipUid.Value);
        if (!spawners.Any())
            return;

        var spawner = spawners.First();
        var playerMob = SpawnPlayer(player, spawner);
        AfterSpawn(playerMob, spawner);

        TeamMessage(targetTeam, Loc.GetString("shipevent-team-newmember", ("name", GetName(playerMob))), color: targetTeam.Color);
    }

    /// <summary>
    /// Spawns player using specified spawner
    /// </summary>
    /// <param name="player">player's session</param>
    /// <param name="spawnerUid">spawner's entity</param>
    /// <returns>player's entity</returns>
    private EntityUid SpawnPlayer(IPlayerSession player, EntityUid spawnerUid)
    {
        if (player.AttachedEntity != null)
        {
            if (EntityManager.TryGetComponent<MindContainerComponent>(player.AttachedEntity, out var mind))
                mind.GhostOnShutdown = false; //to prevent ghost duplication

            EntityManager.DeleteEntity((EntityUid)player.AttachedEntity);
        }

        var spawner = EntityManager.GetComponent<ShipEventSpawnerComponent>(spawnerUid);
        var playerMob = EntityManager.SpawnEntity(spawner.Prototype, Transform(spawnerUid).Coordinates);
        var xform = EntityManager.GetComponent<TransformComponent>(playerMob);
        xform.AttachToGridOrMap();

        playerMob.EnsureComponent<MindContainerComponent>();

        var newMind =  _mindSystem.CreateMind(player.UserId, EntityManager.GetComponent<MetaDataComponent>(playerMob).EntityName);
        _mindSystem.TransferTo(newMind, playerMob);

        HumanoidCharacterProfile profile;
        if (_prefsManager.TryGetCachedPreferences(player.UserId, out var preferences))
            profile = (HumanoidCharacterProfile) preferences.GetProfile(preferences.SelectedCharacterIndex);
        else
            profile = HumanoidCharacterProfile.Random();

        _humanoidAppearanceSystem.LoadProfile(playerMob, profile);

        return playerMob;
    }

    /// <summary>
    /// Sets up roles, HUD, action buttons, team marker & other stuff after spawn
    /// </summary>
    private void AfterSpawn(EntityUid spawnedEntity, EntityUid spawnerEntity)
    {
        if (!spawnedEntity.IsValid())
            return;

        var mind = EntityManager.GetComponentOrNull<MindContainerComponent>(spawnedEntity);
        if (mind?.Mind?.Session == null)
            return;
        var session = mind.Mind.Session;

        ShipEventFaction team = default!;

        if (_mindSystem.HasRole<ShipEventRole>(mind.Mind))
            return;

        if (EntityManager.TryGetComponent<ShipEventFactionMarkerComponent>(spawnerEntity, out var spawnerMarker))
        {
            if (spawnerMarker.Team == null)
                return;
            team = spawnerMarker.Team;

            var playerMarker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(spawnedEntity);
            playerMarker.Team = team;
        }

        Role shipEventRole = new ShipEventRole(mind.Mind);
        _mindSystem.AddRole(mind.Mind, shipEventRole);
        team.AddMember(shipEventRole);

        SetPlayerCharacterName(spawnedEntity, $"{GetName(spawnedEntity)} ({team.Name})");

        SetupActions(spawnedEntity, team, session);

        if (EntityManager.TryGetComponent<MobHUDComponent>(spawnedEntity, out var hud))
        {
            var hudProt = _protMan.Index<MobHUDPrototype>(
                session.ConnectedClient.UserName == team.Captain ? CaptainHUDPrototypeId : HUDPrototypeId).ShallowCopy();
            hudProt.Color = team.Color;
            _hudSys.SetActiveHUDs(hud, new List<MobHUDPrototype> { hudProt });
        }
    }

    /// <summary>
    /// Sets up action buttons for specified player
    /// </summary>
    /// <param name="uid">player's uid</param>
    /// <param name="team">player's team</param>
    /// <param name="session">player's session</param>
    private void SetupActions(EntityUid uid, ShipEventFaction team, IPlayerSession session)
    {
        if (EntityManager.TryGetComponent<ActionsComponent>(uid, out var actComp))
        {
            var teamView = EntityManager.EnsureComponent<ShipEventFactionViewComponent>(uid);
            teamView.ToggleAction = (InstantAction)_protMan.Index<InstantActionPrototype>("ShipEventTeamViewToggle").Clone();
            _actSys.AddAction(uid, teamView.ToggleAction, null, actComp);

            if (team.Captain == session.ConnectedClient.UserName)
            {
                var capMenu = EntityManager.EnsureComponent<ShipEventCaptainMenuComponent>(uid);
                capMenu.ToggleAction = (InstantAction)_protMan.Index<InstantActionPrototype>("ShipEventCaptainMenuToggle").Clone();
                _actSys.AddAction(uid, capMenu.ToggleAction, null, actComp);
            }
        }
    }

    /// <summary>
    /// Creates new faction with all the specified data. Does not spawn ship, if you want to put new team in game right away use CreateTeam
    /// </summary>
    private ShipEventFaction RegisterTeam(string captain, string name, Color color, List<string>? blacklist = null, bool announce = true)
    {
        var teamName = IsValidName(name) ? name : GenerateTeamName();
        var teamColor = IsValidColor(color) ? color : GenerateTeamColor();

        var team = new ShipEventFaction(
            teamName,
            "",
            teamColor,
            captain,
            blacklist: blacklist);


        Teams.Add(team);

        if (announce)
        {
            Announce(Loc.GetString(
                "shipevent-team-add",
                ("teamname", team.Name)));
        }

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
    /// <param name="respawnReason">Message to show in announcement</param>
    /// <param name="announce">Whether to announce respawn</param>
    /// <param name="immediate">If this team should be respawned without delay</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on respawned one</param>
    private void RespawnTeam(ShipEventFaction team, string respawnReason = "", bool announce = true, bool immediate = false, bool killPoints = true)
    {
        if (announce)
        {
            var message = Loc.GetString(
                "shipevent-team-respawn",
                ("teamname", team.Name),
                ("shipname", GetName(team.Ship)),
                ("respawnreason", respawnReason == "" ? Loc.GetString("shipevent-respawn-default") : respawnReason),
                ("respawntime", RespawnDelay / 60));
            Announce(message);
        }

        if (killPoints)
            AddKillPoints(team);

        team.Hits.Clear();

        foreach (var member in team.Members)
        {
            EntityManager.DeleteEntity(team.GetMemberEntity(member));
            if (member.Mind.OwnedEntity != null && member.Mind.Session != null)
                SetupActions(member.Mind.OwnedEntity.Value, team, member.Mind.Session); //so ghosts have team view & other stuff enabled too
        }

        foreach (var marker in GetShipComponents<ShipEventFactionMarkerComponent>(team.Ship))
        {
            if (marker.Team != team)
            {
                var transform = Transform(marker.Owner);
                _formSys.SetParent(transform.Owner, _mapMan.GetMapEntityId(transform.MapID));
                _formSys.SetGridId(marker.Owner, transform, null);
                Dirty(transform);
            }
        }

        if (team.Ship != EntityUid.Invalid)
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

        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>(newShip);
        if (!spawners.Any())
            return;

        SetMarkers(newShip, team);
        SetShipName(newShip, team.ShipName);

        team.Ship = newShip;

        List<IPlayerSession> sessions = new();
        foreach (var member in team.Members)
        {
            var session = GetSession(member.Mind);
            if (session != null)
                sessions.Add(session);
        }

        team.Members.Clear();
        var spawner = spawners.First();
        foreach (var session in sessions)
        {
            var playerMob = SpawnPlayer(session, spawner);
            AfterSpawn(playerMob, spawner);
        }

        team.Respawns++;
        team.ShouldRespawn = false;
    }

    /// <summary>
    /// Removes team entirely
    /// </summary>
    /// <param name="team">Team to remove</param>
    /// <param name="removeReason">Message to show in announcement</param>
    /// <param name="announce">Whether to announce removal</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on removed one</param>
    public void RemoveTeam(ShipEventFaction team, string removeReason = "", bool announce = true, bool killPoints = true)
    {
        if (announce)
        {
            var message = Loc.GetString(
                "shipevent-team-remove",
                ("teamname", team.Name),
                ("shipname", GetName(team.Ship)),
                ("removereason", removeReason == "" ? Loc.GetString("shipevent-remove-default") : removeReason));
            Announce(message);
        }

        if (killPoints)
            AddKillPoints(team);

        foreach (var member in team.Members)
        {
            EntityManager.DeleteEntity(team.GetMemberEntity(member));
        }

        if (team.Ship != EntityUid.Invalid)
            EntityManager.DeleteEntity(team.Ship);

        Teams.Remove(team);
    }

    /// <summary>
    ///     Adds points to other teams for destroying specified one.
    /// </summary>
    /// <param name="team">Team which is destroyed</param>
    private void AddKillPoints(ShipEventFaction team)
    {
        var totalHits = 0;
        foreach ((var killerTeam, var hits) in team.Hits)
        {
            totalHits += hits;
        }

        foreach ((var killerTeam, var hits) in team.Hits)
        {
            double ratio = hits / totalHits;
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

    private int GetMemberLimit()
    {
        int totalMembers = 0;
        int minMembers = 0;
        foreach (var team in Teams)
        {
            totalMembers += team.Members.Count;
            if (team.Members.Count < minMembers || minMembers == 0)
                minMembers = team.Members.Count;
        }

        return minMembers + totalMembers / PlayersPerTeamPlace;
    }

    private void CheckTeams(float deltaTime)
    {
        List<ShipEventFaction> toRemove = new();
        foreach (var team in Teams)
        {
            if (team.ActiveMembers.Count == 0)
            {
                toRemove.Add(team);
                continue;
            }

            if (!team.GetLivingMembersMinds().Any() && team.Members.Any() && !team.ShouldRespawn)
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
                    string newCap = team.ActiveMembers.Count > 0 ? team.ActiveMembers[0].Mind.Session!.ConnectedClient.UserName : "N/A";

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
                        ("points", PointsPerInterval)),
                    color: team.Color);
                team.Points += PointsPerInterval;
                team.LastBonusInterval++;
            }

            team.TimeSinceRemoval += deltaTime;
        }

        foreach (ShipEventFaction team in toRemove)
        {
            RemoveTeam(team, Loc.GetString("shipevent-remove-noplayers"));
        }
    }
}
