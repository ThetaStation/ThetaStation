using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.IdentityManagement;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Theta.MobHUD;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Actions;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.MobHUD;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.UI;
using Content.Shared.Toggleable;
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
    [Dependency] private readonly IdentitySystem _idSys = default!;
    [Dependency] private readonly MapLoaderSystem _mapSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    private readonly Dictionary<EntityUid, string> _shipNames = new();
    private readonly Dictionary<string, int> _projectileDamage = new(); //cached damage for projectile prototypes
    private int _lastTeamNumber;
    private float _teamCheckTimer;
    private float _roundendTimer;

    public float RoundDuration; //in seconds
    public bool TimedRoundEnd = false;

    public float TeamCheckInterval; //in seconds
    public float RespawnDelay; //in seconds

    public int MaxSpawnOffset; //both for ships & obstacles
    public int CollisionCheckRange;

    public int BonusInterval; //in seconds
    public int PointsPerInterval; //points for surviving longer than BonusInterval without respawn

    public float PointsPerHitMultiplier;
    public int PointsPerAssist;
    public int PointsPerKill;

    public string HUDPrototypeId = "ShipeventHUD";

    public bool RuleSelected = false;

    public List<string> ShipTypes = new();

    public List<string> ObstacleTypes = new();

    public MapId TargetMap;

    public List<ShipEventFaction> Teams { get; } = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipEventFactionViewComponent, ToggleActionEvent>(OnView);
        SubscribeLocalEvent<ShipEventFactionViewComponent, ComponentInit>(OnViewInit);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, StartCollideEvent>(OnCollision);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, MobStateChangedEvent>(OnPlayerStateChange);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frametime)
    {
        _teamCheckTimer += frametime;
        _roundendTimer += frametime;
        
        if (_teamCheckTimer > TeamCheckInterval)
        {
            _teamCheckTimer -= TeamCheckInterval;
            CheckTeams(TeamCheckInterval);
        }

        CheckRoundendTimer();
    }

    public void CheckRoundendTimer()
    {
        if (!TimedRoundEnd)
            return;
        
        switch (RoundDuration - _roundendTimer)
        {
            case 600:
               Announce(Loc.GetString("shipevent-roundendtimer-tenmins"));
               break;
            case 300:
                Announce(Loc.GetString("shipevent-roundendtimer-fivemins"));
                break;
            case 60:
                Announce(Loc.GetString("shipevent-roundendtimer-onemin"));
                break;
            case <= 0:
                _ticker.EndRound();
                break;
        }
    }

    public void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Teams.Clear();
        _lastTeamNumber = 0;
    }

    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
        if (!RuleSelected || !Teams.Any())
            return;


        var winner = Teams.First();
        args.AddLine(Loc.GetString("shipevent-roundend-heading"));
        foreach (var team in Teams)
        {
            if (team.Points > winner.Points)
                winner = team;

            args.AddLine(Loc.GetString("shipevent-roundend-team",
                ("name", team.Name),
                ("color", team.Color),
                ("shipname", _shipNames[team.Ship]),
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

    private void OnView(EntityUid entity, ShipEventFactionViewComponent component, ToggleActionEvent args)
    {
        if (!RuleSelected)
            return;

        var session = GetSession(entity);
        if (session == null)
            return;

        List<ShipTeamForTeamViewState> teamsInfo = new();
        foreach (var team in Teams)
        {
            teamsInfo.Add(new ShipTeamForTeamViewState
            {
                Name = team.Name,
                Color = team.Color,
                ShipName = team.ShouldRespawn ? null : _shipNames[team.Ship],
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

    private void OnViewInit(EntityUid entity, ShipEventFactionViewComponent view, ComponentInit args)
    {
        if (EntityManager.TryGetComponent<ActionsComponent>(entity, out var actComp))
            _actSys.AddAction(entity, view.ToggleAction, null, actComp);
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
        
        var spawners = GetShipComponents<ShipEventSpawnerComponent>((EntityUid)ship);

        if (!spawners.Any())
        {
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-respawnfailed"), session);
            return;
        }

        var spawner = spawners.First();
        var playerMob = SpawnPlayer(session, spawner);
        AfterSpawn(playerMob, spawner);
    }

    /// <summary>
    /// Does everything needed to create a new team, from faction creation to ship spawning.
    /// </summary>
    /// <param name="captainSession"></param>
    /// <param name="name"></param>
    /// <param name="color"></param>
    /// <param name="blacklist"></param>
    public void CreateTeam(ICommonSession captainSession, string name, string color,
        List<string>? blacklist = null)
    {
        if (!RuleSelected)
            return;

        var newShip = RandomPosSpawn(_random.Pick(ShipTypes));
        var spawners = GetShipComponents<ShipEventSpawnerComponent>(newShip);
        if (!spawners.Any())
            return;

        var team = RegisterTeam(newShip, captainSession.ConnectedClient.UserName, name, color, blacklist);
        SetMarkers(newShip, team);

        var spawner = spawners.First();
        var playerMob = SpawnPlayer((IPlayerSession) captainSession, spawner);
        AfterSpawn(playerMob, spawner);
    }

    /// <summary>
    /// Adds player to faction (by name), spawn him on ship & does all other required stuff
    /// </summary>
    /// <param name="player">player's session</param>
    /// <param name="teamName">name of the team</param>
    public void JoinTeam(IPlayerSession player, string teamName)
    {
        EntityUid? shipUid = null;
        ShipEventFaction teamFaction = default!;
        foreach (var team in Teams)
        {
            if (team.Name == teamName)
            {
                teamFaction = team;
                shipUid = team.Ship;
            }
        }

        if (shipUid == null)
            return;

        var spawners = GetShipComponents<ShipEventSpawnerComponent>(shipUid.Value);
        if (!spawners.Any())
            return;

        var spawner = spawners.First();
        var playerMob = SpawnPlayer(player, spawner);
        AfterSpawn(playerMob, spawner);
        
        TeamMessage(teamFaction, Loc.GetString("shipevent-team-newmember", ("name", GetName(playerMob))),
            color: Color.FromHex(teamFaction.Color));
    }

    private EntityUid SpawnPlayer(IPlayerSession player, EntityUid spawnerUid)
    {
        if (player.AttachedEntity != null)
        {
            var uid = player.AttachedEntity;
            player.DetachFromEntity();
            EntityManager.DeleteEntity((EntityUid)uid);
        }

        var spawner = EntityManager.GetComponent<ShipEventSpawnerComponent>(spawnerUid);
        var playerMob = EntityManager.SpawnEntity(spawner.Prototype, Transform(spawnerUid).Coordinates);
        var xform = EntityManager.GetComponent<TransformComponent>(playerMob);
        xform.AttachToGridOrMap();
        
        playerMob.EnsureComponent<MindComponent>();
        var newMind = new Mind.Mind(player.UserId)
        {
            CharacterName = EntityManager.GetComponent<MetaDataComponent>(playerMob).EntityName
        };
        newMind.ChangeOwningPlayer(player.UserId);
        newMind.TransferTo(playerMob);

        return playerMob;
    }

    private void AfterSpawn(EntityUid spawnedEntity, EntityUid spawnerEntity)
    {
        if (!spawnedEntity.IsValid())
            return;

        var session = GetSession(spawnedEntity);
        if (session == null)
            return;

        ShipEventFaction team = default!;

        if (EntityManager.TryGetComponent<ShipEventFactionMarkerComponent>(spawnerEntity, out var teamMarker))
        {
            if (teamMarker.Team == null)
                return;

            team = teamMarker.Team;

            if (team.Blacklist != null)
            {
                if (team.Blacklist.Contains(session.ConnectedClient.UserName))
                {
                    _chatSys.SendSimpleMessage(Loc.GetString("shipevent-blacklist"), session);
                    EntityManager.DeleteEntity(spawnedEntity);
                    return;
                }
            }

            AddToTeam(spawnedEntity, team);

            var marker = EntityManager.GetComponent<ShipEventFactionMarkerComponent>(spawnedEntity);
            marker.Team = team;
        }

        if (EntityManager.TryGetComponent<MobHUDComponent>(spawnedEntity, out var hud))
        {
            var hudProt = _protMan.Index<MobHUDPrototype>(HUDPrototypeId).ShallowCopy();
            hudProt.Color = team.Color;
            _hudSys.SetActiveHUDs(hud, new List<MobHUDPrototype> { hudProt });
        }
    }

    private void OnCollision(EntityUid entity, ShipEventFactionMarkerComponent component, ref StartCollideEvent args)
    {
        if (component.Team == null)
            return;

        if (EntityManager.TryGetComponent(entity, out ProjectileComponent? projComp))
        {
            if (EntityManager.TryGetComponent<ShipEventFactionMarkerComponent>(
                    Transform(args.OtherFixture.Body.Owner).GridUid, out var marker))
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
    /// Adds specified entity to faction (does NOT set up markers/spawn player, use JoinTeam if you want to put player in game right away)
    /// </summary>
    /// <param name="entity">entity to add</param>
    /// <param name="team">faction</param>
    private void AddToTeam(EntityUid entity, ShipEventFaction team)
    {
        if (EntityManager.TryGetComponent<MindComponent>(entity, out var mindComp))
        {
            if (!mindComp.HasMind)
                return;

            if (mindComp.Mind!.HasRole<ShipEventRole>())
                return;

            SetName(entity, GetName(entity) + $"({team.Name})");

            Role shipEventRole = new ShipEventRole(mindComp.Mind!);
            mindComp.Mind!.AddRole(shipEventRole);
            team.AddMember(shipEventRole);
        }
    }

    /// <summary>
    /// Creates new faction with all the specified data. Does not spawn ship, if you want to put new team in game right away use CreateTeam
    /// </summary>
    private ShipEventFaction RegisterTeam(EntityUid shipEntity, string captain, string name = "", string color = "",
        List<string>? blacklist = null, bool silent = false)
    {
        var shipName = GetName(shipEntity);
        var teamName = IsValidName(name) ? name : GenerateTeamName();
        var teamColor = IsValidColor(color) ? color : GenerateTeamColor();

        var team = new ShipEventFaction(
            teamName,
            "",
            teamColor,
            shipEntity,
            captain,
            blacklist: blacklist);

        Teams.Add(team);
        _shipNames[shipEntity] = shipName;

        if (!silent)
        {
            Announce(Loc.GetString(
                "shipevent-team-add",
                ("teamname", team.Name),
                ("shipname", shipName)));
        }

        SetMarkers(shipEntity, team);

        return team;
    }

    /// <summary>
    ///     Sets ShipEventFactionMarker components teams on spawner, cannons, etc.
    /// </summary>
    /// <param name="shipEntity">Ship in question</param>
    /// <param name="team">Team in question</param>
    public void SetMarkers(EntityUid shipEntity, ShipEventFaction team)
    {
        var spawners = GetShipComponents<ShipEventSpawnerComponent>(shipEntity);
        foreach (var spawner in spawners)
        {
            var marker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(spawner);
            marker.Team = team;
        }

        var cannons = GetShipComponents<CannonComponent>(shipEntity);
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
    /// <param name="silent">Whether to announce respawn</param>
    /// <param name="immediate">If this team should be respawned without delay</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on respawned one</param>
    private void RespawnTeam(ShipEventFaction team, string respawnReason = "", bool silent = false,
        bool immediate = false, bool killPoints = true)
    {
        if (!silent)
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

        var oldShipName = GetName(team.Ship);
        _shipNames.Remove(team.Ship);
        foreach (var member in team.Members)
        {
            EntityManager.DeleteEntity(team.GetMemberEntity(member));
        }

        if (team.Ship != EntityUid.Invalid)
            EntityManager.DeleteEntity(team.Ship);

        team.Ship = EntityUid.Invalid;

        if (immediate)
            ImmediateRespawn(team, oldShipName);
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
    private void ImmediateRespawn(ShipEventFaction team, string oldShipName = "")
    {
        var newShip = RandomPosSpawn(_random.Pick(ShipTypes));

        var spawners = GetShipComponents<ShipEventSpawnerComponent>(newShip);
        if (!spawners.Any())
            return;

        SetMarkers(newShip, team);

        if (oldShipName != "")
            SetName(newShip, oldShipName);

        team.Ship = newShip;
        _shipNames[team.Ship] = GetName(newShip);

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
    ///     Removes team entirely
    /// </summary>
    /// <param name="team">Team to remove</param>
    /// <param name="removeReason">Message to show in announcement</param>
    /// <param name="silent">Whether to announce removal</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on removed one</param>
    private void RemoveTeam(ShipEventFaction team, string removeReason = "", bool silent = false,
        bool killPoints = true)
    {
        if (!silent)
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

        _shipNames.Remove(team.Ship);
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

    private void CheckTeams(float deltaTime)
    {
        foreach (var team in Teams)
        {
            if (!team.GetLivingMembersMinds().Any() && team.Members.Any() && !team.ShouldRespawn)
            {
                RespawnTeam(
                    team,
                    Loc.GetString("shipevent-respawn-dead"));
                team.TimeSinceRemoval = 0;
                break;
            }

            if (!GetShipComponents<ShuttleConsoleComponent>(team.Ship).Any() && !team.ShouldRespawn)
            {
                RespawnTeam(
                    team,
                    Loc.GetString("shipevent-respawn-tech"));
                team.TimeSinceRemoval = 0;
                break;
            }

            if (_shipNames.ContainsKey(team.Ship))
            {
                var shipName = GetName(team.Ship);
                if (shipName != _shipNames[team.Ship])
                {
                    var message = Loc.GetString(
                        "shipevent-team-shiprename",
                        ("teamname", team.Name),
                        ("oldname", _shipNames[team.Ship]),
                        ("newname", shipName));
                    Announce(message);
                    _shipNames[team.Ship] = shipName;
                }
            }

            if (!_playerMan.TryGetSessionByUsername(team.Captain, out _))
            {
                if (team.Members.Any())
                {
                    string newCap = "";
                    for (int c = 0; c < 100; c++)
                    {
                        var newCapRole = _random.Pick(team.Members);
                        if (newCapRole.Mind.Session != null)
                        {
                            newCap = newCapRole.Mind.Session.ConnectedClient.UserName;
                            break;
                        }
                    }

                    TeamMessage(team,
                        Loc.GetString("shipevent-team-captainchange", ("oldcap", team.Captain),
                            ("newcap", newCap)), color: Color.FromHex(team.Color));
                    team.Captain = newCap;
                }
            }

            if (team.ShouldRespawn && team.TimeSinceRemoval > RespawnDelay)
            {
                TeamMessage(team, Loc.GetString("shipevent-team-respawnnow"), color: Color.FromHex(team.Color));
                ImmediateRespawn(team);
            }

            if (team.BonusIntervalTimer > BonusInterval)
            {
                TeamMessage(team,
                    Loc.GetString("shipevent-team-bonusinterval",
                        ("time", BonusInterval / 60),
                        ("points", PointsPerInterval)),
                    color: Color.FromHex(team.Color));
                team.Points += PointsPerInterval;
                team.BonusIntervalTimer -= BonusInterval;
            }

            team.TimeSinceRemoval += deltaTime;
            if (!team.ShouldRespawn)
                team.BonusIntervalTimer += deltaTime;
        }
    }

    public List<EntityUid> CreateObstacles(int amount)
    {
        List<EntityUid> spawnedObstacles = new();
        for (int i = 0; i < amount; i++)
        {
            var obstacleUid = RandomPosSpawn(_random.Pick(ObstacleTypes));
            _shuttleSystem.AddIFFFlag(obstacleUid, IFFFlags.HideLabel);
            _shuttleSystem.SetIFFColor(obstacleUid, Color.Gold);
            spawnedObstacles.Add(obstacleUid);
        }

        return spawnedObstacles;
    }
}
