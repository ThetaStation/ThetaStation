using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.IdentityManagement;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Theta.MobHUD;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Actions;
using Content.Shared.GameTicking;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actSys = default!;
    [Dependency] private readonly ChatSystem _chatSys = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly MobHUDSystem _hudSys = default!;
    [Dependency] private readonly IdentitySystem _idSys = default!;
    [Dependency] private readonly MapLoaderSystem _mapSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;

    private readonly Dictionary<EntityUid, string> _shipNames = new();
    private readonly Dictionary<string, int> _projectileDamage = new(); //cached damage for projectile prototypes
    private int _lastTeamNumber;
    private float _teamCheckTimer;

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
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, GhostRoleSpawnerUsedEvent>(OnSpawn);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, StartCollideEvent>(OnCollision);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, TeamCreationRequest>(OnTeamCreationRequest);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frametime)
    {
        if (_teamCheckTimer < TeamCheckInterval)
        {
            _teamCheckTimer += frametime;
            return;
        }

        _teamCheckTimer -= TeamCheckInterval;
        CheckTeams(TeamCheckInterval);
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

        List<TeamState> teamsInfo = new();
        foreach (var team in Teams)
        {
            teamsInfo.Add(new TeamState
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
        if (_entMan.TryGetComponent<ActionsComponent>(entity, out var actComp))
            _actSys.AddAction(entity, view.ToggleAction, null, actComp);
    }

    private void OnTeamCreationRequest(EntityUid entity, ShipEventFactionMarkerComponent component,
        TeamCreationRequest args)
    {
        if (!RuleSelected)
            return;

        List<string> _blacklist = new();

        if (!IsValidName(args.Name))
        {
            if (_uiSys.TryGetUi(entity, args.UiKey, out var bui))
            {
                _uiSys.SetUiState(bui,
                    new TeamCreationBoundUserInterfaceState(
                        Loc.GetString("shipevent-teamcreation-response-invalidname")));
            }

            return;
        }

        var _color = string.Empty;
        if (args.Color.Any())
        {
            if (!IsValidColor(args.Color))
            {
                if (_uiSys.TryGetUi(entity, args.UiKey, out var bui))
                {
                    _uiSys.SetUiState(bui,
                        new TeamCreationBoundUserInterfaceState(
                            Loc.GetString("shipevent-teamcreation-response-invalidcolor")));
                }

                return;
            }

            _color = args.Color;
        }
        else
            _color = Color.White.ToHex();

        if (args.Blacklist.Any())
        {
            _blacklist = args.Blacklist.Split(",").ToList();
            _blacklist.Select(ckey => ckey.Trim());
        }

        if (_blacklist.Contains(args.Session.ConnectedClient.UserName))
        {
            if (_uiSys.TryGetUi(entity, args.UiKey, out var bui))
            {
                _uiSys.SetUiState(bui,
                    new TeamCreationBoundUserInterfaceState(
                        Loc.GetString("shipevent-teamcreation-response-blacklistself")));
            }

            return;
        }

        if (args.Session.AttachedEntity == null)
            return;

        var newShip = RandomPosSpawn(_random.Pick(ShipTypes));
        var spawners = GetShipComponents<GhostRoleMobSpawnerComponent>(newShip);
        if (!spawners.Any())
            return;

        var team = CreateTeam(newShip, args.Session.ConnectedClient.UserName, args.Name, _color, _blacklist);
        SetMarkers(newShip, team);

        _entMan.DeleteEntity((EntityUid) args.Session.AttachedEntity);
        _entMan.GetComponent<GhostRoleMobSpawnerComponent>(spawners.First()).Take((IPlayerSession) args.Session);
    }

    private void OnSpawn(EntityUid entity, ShipEventFactionMarkerComponent component, GhostRoleSpawnerUsedEvent args)
    {
        var session = GetSession(entity);
        if (session == null)
            return;

        ShipEventFaction team = default!;

        if (_entMan.TryGetComponent<ShipEventFactionMarkerComponent>(args.Spawner, out var teamMarker))
        {
            if (teamMarker.Team == null)
                return;

            team = teamMarker.Team;

            if (team.Blacklist != null)
            {
                if (team.Blacklist.Contains(session.ConnectedClient.UserName))
                {
                    _chatSys.SendSimpleMessage(Loc.GetString("shipevent-blacklist"), session);
                    _entMan.DeleteEntity(entity);
                    return;
                }
            }

            AddToTeam(args.Spawned, team);
            component.Team = team;
            _entMan.GetComponent<GhostRoleMobSpawnerComponent>(args.Spawner)
                .SetCurrentTakeovers(team.Members.Count);
        }

        if (_entMan.TryGetComponent<MobHUDComponent>(args.Spawned, out var hud))
        {
            var hudProt = _protMan.Index<MobHUDPrototype>(HUDPrototypeId);
            hudProt.Color = team.Color;
            _hudSys.SetActiveHUDs(hud, new List<MobHUDPrototype> { hudProt });
        }
    }

    private void OnCollision(EntityUid entity, ShipEventFactionMarkerComponent component, ref StartCollideEvent args)
    {
        if (component.Team == null)
            return;

        if (_entMan.TryGetComponent(entity, out ProjectileComponent? projComp))
        {
            if (_entMan.TryGetComponent<ShipEventFactionMarkerComponent>(
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

    private void AddToTeam(EntityUid entity, ShipEventFaction team)
    {
        if (_entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
        {
            if (!mindComp.HasMind)
                return;

            if (mindComp.Mind!.HasRole<ShipEventRole>())
                return;

            SetName(entity, GetName(entity) + $"({team.Name})");

            Role shipEventRole = new ShipEventRole(mindComp.Mind!);
            mindComp.Mind!.AddRole(shipEventRole);
            team.AddMember(shipEventRole);
            TeamMessage(team, Loc.GetString("shipevent-team-newmember", ("name", GetName(entity))),
                color: Color.FromHex(team.Color));
        }
    }

    public ShipEventFaction CreateTeam(EntityUid shipEntity, string captain, string name = "", string color = "",
        List<string>? blacklist = null, bool silent = false)
    {
        var shipName = GetName(shipEntity);
        var teamName = IsValidName(name) ? name : GenerateTeamName();
        var teamColor = IsValidColor(color) ? color : GenerateTeamColor();

        var team = new ShipEventFaction(
            teamName,
            "/Textures/Theta/ShipEvent/ShipFactionIcon.rsi",
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
        var spawners = GetShipComponents<GhostRoleMobSpawnerComponent>(shipEntity);
        foreach (var spawner in spawners)
        {
            var marker = _entMan.EnsureComponent<ShipEventFactionMarkerComponent>(spawner);
            marker.Team = team;
        }

        var cannons = GetShipComponents<CannonComponent>(shipEntity);
        foreach (var cannon in cannons)
        {
            var marker = _entMan.EnsureComponent<ShipEventFactionMarkerComponent>(cannon);
            marker.Team = team;
        }

        var markerShip = _entMan.EnsureComponent<ShipEventFactionMarkerComponent>(shipEntity);
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
            _entMan.DeleteEntity(team.GetMemberEntity(member));
        }

        if (team.Ship != EntityUid.Invalid)
            _entMan.DeleteEntity(team.Ship);

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

        var spawners = GetShipComponents<GhostRoleMobSpawnerComponent>(newShip);
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
        foreach (var session in sessions)
        {
            _entMan.GetComponent<GhostRoleMobSpawnerComponent>(spawners.First()).Take(session);
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
            _entMan.DeleteEntity(team.GetMemberEntity(member));
        }

        if (team.Ship != EntityUid.Invalid)
            _entMan.DeleteEntity(team.Ship);

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
                    for(int c = 0; c < 100; c++)
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
