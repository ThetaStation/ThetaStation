using System.Linq;
using System.Numerics;
using System.Threading.Channels;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Corvax.RoundNotifications;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Humanoid;
using Content.Server.IdentityManagement;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Preferences.Managers;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.VoiceMask;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.DebrisGeneration;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Content.Server.Theta.MobHUD;
using Content.Server.Theta.NiceColors;
using Content.Server.Theta.NiceColors.ColorPalettes;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Content.Shared.Movement.Components;
using Content.Shared.Preferences;
using Content.Shared.Projectiles;
using Content.Shared.Radio;
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
using Robust.Shared.Utility;
using Robust.Shared.Network;
using TerraFX.Interop.Windows;


namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _ticker = default!;

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

    [Dependency] private readonly ShipEventFactionSystem _shipEventSystem = default!;

    [Dependency] private readonly INetManager _netMan = default!;

    //used when setting up buttons for ghosts, in cases when mind from shipevent agent is transferred to null and not to ghost entity directly
    private Dictionary<IPlayerSession, ShipEventFaction> lastTeamLookup = new();

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

    public ColorPalette ColorPalette = new ShipEventPalette();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShipEventFactionMarkerComponent, ShipEventTeamViewToggleEvent>(OnViewToggle);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, ShipEventCaptainMenuToggleEvent>(OnCapMenuToggle);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, ShipEventReturnToLobbyEvent>(OnReturnToLobbyAction);

        SubscribeLocalEvent<ShipEventFactionMarkerComponent, GenericWarningYesPressedMessage>(ReturnToLobbyPlayer);

        SubscribeLocalEvent<ShipEventFactionMarkerComponent, StartCollideEvent>(OnCollision);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, MobStateChangedEvent>(OnPlayerStateChange);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, MindTransferredMessage>(OnPlayerTransfer);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, EntitySpokeEvent>(OnTeammateSpeak);

        SubscribeLocalEvent<ShipEventLootboxSpawnTriggerComponent, UseInHandEvent>(OnLootboxSpawnTriggered);
        SubscribeLocalEvent<ShipEventPointStorageComponent, UseInHandEvent>(OnPointStorageTriggered);

        SubscribeAllEvent<ShuttleConsoleChangeShipNameMessage>(OnShipNameChange); //un-directed event since we will have duplicate subscriptions otherwise
        SubscribeAllEvent<GetShipPickerInfoMessage>(OnShipPickerInfoRequest);
        SubscribeAllEvent<BoundsOverlayInfoRequest>(OnBoundsOverlayInfoRequest);
        SubscribeAllEvent<LootboxInfoRequest>(OnLootboxInfoRequest);

        InitializeCaptainMenu();

        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundEndDiscordTextAppendEvent>(OnRoundEndDiscord);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnTeammateSpeak(EntityUid uid, ShipEventFactionMarkerComponent component, EntitySpokeEvent args)
    {
        // throw new NotImplementedException();

        var channel = args.Channel;
        if (channel == null) return;

        if (!EntityManager.HasComponent<WearingHeadsetComponent>(uid) && channel.ID != "Common")
            return;

        var name = TryComp(args.Source, out VoiceMaskComponent? mask) && mask.Enabled
        ? mask.VoiceName
        : MetaData(args.Source).EntityName;

        name = FormattedMessage.EscapeText(name);

        var chat = new ChatMessage(
        ChatChannel.Radio,
        args.Message,
        Loc.GetString("chat-radio-message-wrap", ("color", channel.Color), ("channel", $"\\[{channel.LocalizedName}\\]"), ("name", name), ("message", FormattedMessage.EscapeText(args.Message))),
        EntityUid.Invalid);
        var chatMsg = new MsgChatMessage { Message = chat };
        var ev = new RadioReceiveEvent(args.Message, args.Source, channel, chatMsg);

        var team = _shipEventSystem.TryGetTeamByMember(uid);
        if (team == null) return;

        var list = team.Members;
        foreach ( var member in list )
        {
            var MemberEntity = team.GetMemberEntity(member);
            //RaiseLocalEvent(MemberEntity, ref ev);
            if (TryComp(MemberEntity, out ActorComponent? actor))
                _netMan.ServerSendMessage(chatMsg, actor.PlayerSession.ConnectedClient);
        }

        args.Channel = null;
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

        RuleSelected = false;

        ShipTypes.Clear();
        TargetMap = MapId.Nullspace;

        Teams.Clear();
        lastTeamLookup.Clear();

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

    private void OnViewToggle(EntityUid entity, ShipEventFactionMarkerComponent marker,
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
            UserInterfaceSystem.SetUiState(bui, new TeamViewBoundUserInterfaceState(teamsInfo));
        }
    }

    private void OnCapMenuToggle(EntityUid uid, ShipEventFactionMarkerComponent marker, ShipEventCaptainMenuToggleEvent args)
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

        foreach (var team in Teams)
        {
            if (team.Captain != session.ConnectedClient.UserName)
                continue;
            _uiSys.TrySetUiState(uid,
                uiKey,
                new ShipEventCaptainMenuBoundUserInterfaceState(team.GetMemberUserNames(), team.ChosenShipType, team.JoinPassword, team.MaxMembers)
                );
            break;
        }

        _uiSys.TryOpen(uid, uiKey, session);
    }

    private void OnReturnToLobbyAction(EntityUid uid, ShipEventFactionMarkerComponent marker, ShipEventReturnToLobbyEvent args)
    {
        var session = GetSession(args.Performer);
        if (session == null)
            return;

        if(!_uiSys.TryGetUi(uid, GenericWarningUiKey.ShipEventKey, out var bui))
            return;
        UserInterfaceSystem.SetUiState(bui, new GenericWarningBoundUserInterfaceState
            {
                WarningLoc = "generic-warning-window-warning-to-lobby",
            }
        );
        _uiSys.OpenUi(bui, session);
    }

    private void ReturnToLobbyPlayer(EntityUid uid, ShipEventFactionMarkerComponent component, GenericWarningYesPressedMessage args)
    {
        _ticker.Respawn((IPlayerSession) args.Session);
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

        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>((EntityUid) ship);

        if (!spawners.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-respawnfailed"), session);
            return;
        }

        var spawner = spawners.First();
        var playerMob = SpawnPlayer(session, spawner);
        AfterSpawn(playerMob, spawner);
    }

    private void OnPlayerTransfer(EntityUid uid, ShipEventFactionMarkerComponent marker, MindTransferredMessage args)
    {
        if (args.NewEntity == null)
        {
            if (args.Mind.Session == null || marker.Team == null)
                return;

            lastTeamLookup[args.Mind.Session] = marker.Team;
            return;
        }

        //'null' ghost case
        if (args.NewEntity == uid)
        {
            IPlayerSession? session = GetSession(uid);
            if (session == null)
                return;

            if (lastTeamLookup.TryGetValue(session, out ShipEventFaction? team))
            {
                marker.Team = team;
                lastTeamLookup.Remove(session);
            }
        }

        if (EntityManager.HasComponent<GhostComponent>(args.NewEntity))
        {
            var newMarker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(args.NewEntity.Value);
            newMarker.Team = marker.Team;
            SetupActions(args.NewEntity.Value, newMarker.Team, GetSession(args.NewEntity.Value), true);
        }
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
    public void CreateTeam(ICommonSession captainSession, string name, ShipTypePrototype? initialShipType,
        string? password, int maxMembers)
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

        var color = ColorPalette.GetNextColor();
        var team = RegisterTeam(captainSession.ConnectedClient.UserName, name, color);
        team.ChosenShipType = shipType;
        team.Ship = newShip;
        team.JoinPassword = password;
        team.MaxMembers = maxMembers;
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
    /// <param name="password">password of the team</param>
    public void JoinTeam(IPlayerSession player, string teamName, string? password)
    {
        var shipUid = EntityUid.Invalid;
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

        if (targetTeam.JoinPassword != password)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-incorrect-password"), player);
            return;
        }

        if (targetTeam.MaxMembers != 0 && targetTeam.Members.Count >= targetTeam.MaxMembers)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-memberlimit"), player);
            return;
        }

        if (shipUid == EntityUid.Invalid)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-ship-destroyed"), player);
            return;
        }

        var spawners = GetShipComponentHolders<ShipEventSpawnerComponent>(shipUid);
        if (!spawners.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-spawner-destroyed"), player);
            return;
        }

        var spawner = spawners.First();
        var playerMob = SpawnPlayer(player, spawner);
        AfterSpawn(playerMob, spawner);

        TeamMessage(targetTeam, Loc.GetString("shipevent-team-newmember", ("name", GetName(playerMob))));
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

            EntityManager.QueueDeleteEntity((EntityUid) player.AttachedEntity);
        }

        var spawner = EntityManager.GetComponent<ShipEventSpawnerComponent>(spawnerUid);
        var playerMob = EntityManager.SpawnEntity(spawner.Prototype, Transform(spawnerUid).Coordinates);
        var xform = EntityManager.GetComponent<TransformComponent>(playerMob);
        xform.AttachToGridOrMap();

        playerMob.EnsureComponent<MindContainerComponent>();

        var newMind = _mindSystem.CreateMind(player.UserId, EntityManager.GetComponent<MetaDataComponent>(playerMob).EntityName);
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

        EnsureComp<AutoOrientComponent>(spawnedEntity);

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
    /// <param name="isGhost">player is ghost?</param>
    private void SetupActions(EntityUid uid, ShipEventFaction? team, IPlayerSession? session, bool isGhost = false)
    {
        var teamViewToggle = _protMan.Index<InstantActionPrototype>("ShipEventTeamViewToggle");
        _actSys.AddAction(uid, new InstantAction(teamViewToggle), null);

        if (team != null && session != null && team.Captain == session.ConnectedClient.UserName)
        {
            var capMenuToggle = _protMan.Index<InstantActionPrototype>("ShipEventCaptainMenuToggle");
            _actSys.AddAction(uid, new InstantAction(capMenuToggle), null);
        }

        if (isGhost)
        {
            var retToLobbyAction = _protMan.Index<InstantActionPrototype>("ShipEventReturnToLobbyAction");
            _actSys.AddAction(uid, new InstantAction(retToLobbyAction), null);
        }
    }

    /// <summary>
    /// Creates new faction with all the specified data. Does not spawn ship, if you want to put new team in game right away use CreateTeam
    /// </summary>
    private ShipEventFaction RegisterTeam(string captain, string name, Color color, bool announce = true)
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
                ("respawnreason", respawnReason == "" ? Loc.GetString("shipevent-respawn-default") : respawnReason),
                ("respawntime", RespawnDelay / 60));
            TeamMessage(team, message);
        }

        if (killPoints)
            AddKillPoints(team);

        team.Hits.Clear();

        foreach (var member in team.Members)
        {
            EntityManager.QueueDeleteEntity(team.GetMemberEntity(member));
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
    /// <param name="killPoints">Whether to add points to other teams for hits on removed one</param>
    public void RemoveTeam(ShipEventFaction team, string removeReason = "", bool killPoints = true)
    {
        if (killPoints)
            AddKillPoints(team);

        foreach (var member in team.Members)
        {
            EntityManager.QueueDeleteEntity(team.GetMemberEntity(member));
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
                        ("points", PointsPerInterval)));
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
