using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.IdentityManagement;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Projectiles;
using Content.Shared.Toggleable;
using Content.Shared.Theta.ShipEvent;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class ShipEventFaction : PlayerFaction
{
    public int Respawns;
    public int Kills;
    public int Assists;
    public int Points;
    public float TimeSinceRemoval; //time since last removal (respawn)
    public float BonusIntervalTimer; //used to add bonus points for surviving long enough
    public bool ShouldRespawn; //whether this team is currently waiting for respawn

    public Dictionary<ShipEventFaction, int> Hits = new(); //hits from other teams, not vice-versa
    public EntityUid Captain;
    public EntityUid Ship;

    public ShipEventFaction(string name, string iconPath, EntityUid ship, EntityUid captain, int points = 0) : base(name, iconPath)
    {
        Ship = ship;
        Captain = captain;
        Points = points;
    }
}


public sealed class ShipEventFactionSystem : EntitySystem
{
	[Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly MapLoaderSystem _mapSys = default!;
    [Dependency] private readonly ChatSystem _chatSys = default!;
    [Dependency] private readonly IdentitySystem _idSys = default!;
    [Dependency] private readonly ActionsSystem _actSys = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public int PointsPerHit = 1;
    public int PointsPerKill = 100;
    public int PointsPerAssist = 25;
    public int PointsPerInterval = 25; //points for surviving longer than BonusInterval without respawn
    public int BonusInterval = 1200; //in seconds
    public int RespawnDelay = 60; //in seconds

    private const float TeamCheckInterval = 5;
    private float _teamCheckTimer;
    private int _lastTeamNumber;

    public List<string> ShipTypes = new()
    {
        "/Maps/Shuttles/ship_test_1.yml"
    };

	public List<ShipEventFaction> Teams => _teams;
    private List<ShipEventFaction> _teams = new();
    private Dictionary<EntityUid, string> _shipNames = new();

    public MapId TargetMap;
    public int MaxSpawnOffset = 500;

    public override void Initialize()
	{
		base.Initialize();
        SubscribeLocalEvent<ShipEventFactionViewComponent, ToggleActionEvent>(OnView);
        SubscribeLocalEvent<ShipEventFactionViewComponent, ComponentInit>(OnViewInit);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, GhostRoleSpawnerUsedEvent>(OnSpawn);
        SubscribeLocalEvent<ShipEventFactionMarkerComponent, StartCollideEvent>(OnCollision);
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

	private void OnSpawn(EntityUid entity, ShipEventFactionMarkerComponent component, GhostRoleSpawnerUsedEvent args)
    {
        EntityUid shipEntity = GetSpawnerShip(args.Spawner);
        if (shipEntity == EntityUid.Invalid) { return; }

        if (TryGetTeam(shipEntity, out var team))
        {
            AddToTeam(args.Spawned, team);
            component.Team = team;
            _entMan.GetComponent<GhostRoleMobSpawnerComponent>(args.Spawner).SetCurrentTakeovers(team.Members.Count);
        }
    }

    private void OnView(EntityUid entity, ShipEventFactionViewComponent component, ToggleActionEvent args)
    {
        string result = $"\n{Loc.GetString("shipevent-teamview-heading")}";
        result += $"\n{Loc.GetString("shipevent-teamview-heading2")}";
        foreach (ShipEventFaction team in _teams)
        {
            if (team.ShouldRespawn) { result += $"\n '{team.Name}' - N/A - N/A - {team.Points}"; }
            else{ result += $"\n'{team.Name}' - '{_shipNames[team.Ship]}' - {team.GetLivingMembersMinds().Count} - {team.Points}"; }
        }

        Enum uiKey = TeamViewUiKey.Key;
        IPlayerSession? session = GetSession(entity);
        if (session == null) { return; }

        if (!_uiSys.IsUiOpen(entity, uiKey))
        {
            if (_uiSys.TryGetUi(entity, uiKey, out var bui))
            {
                _uiSys.OpenUi(bui, session);
                _uiSys.SetUiState(bui, new TeamViewBoundUserInterfaceState(result));
            }
        }
    }

    private void OnViewInit(EntityUid entity, ShipEventFactionViewComponent view, ComponentInit args)
    {
        if (_entMan.TryGetComponent<ActionsComponent>(entity, out var actComp)) { _actSys.AddAction(entity, view.ToggleAction, null, actComp); }
    }

    private void OnCollision(EntityUid entity, ShipEventFactionMarkerComponent component, ref StartCollideEvent args)
    {
        if (component.Team == null) { return; }

        //todo: remove Body.Owner by adding other fixture entity into event itself (or use component query alternatives when they will be ready)
        if (_entMan.TryGetComponent<ProjectileComponent>(args.OtherFixture.Body.Owner, out ProjectileComponent? projComp))
        {
            if(_entMan.TryGetComponent<ShipEventFactionMarkerComponent>(Transform(projComp.Shooter).GridUid, out ShipEventFactionMarkerComponent? markComp))
            {
                if (markComp.Team == null || markComp.Team == component.Team) { return; }

                component.Team.Hits[markComp.Team]++;
                markComp.Team.Points += PointsPerHit;
            }
        }
    }

    public void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Teams.Clear();
        _lastTeamNumber = 0;
    }

    private void AddToTeam(EntityUid entity, ShipEventFaction team)
	{
        if (_entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
		{
            if (!mindComp.HasMind) { return; }
            if (mindComp.Mind!.HasRole<ShipEventRole>()) { return; }

            SetName(entity, GetName(entity) + $"({team.Name})");

			Role shipEventRole = new ShipEventRole(mindComp.Mind!);
			mindComp.Mind!.AddRole(shipEventRole);
            team.AddMember(shipEventRole);
            TeamMessage(team, Loc.GetString("shipevent-team-newmember", ("name", GetName(entity))), color: Color.Magenta);
        }
    }

    public ShipEventFaction CreateTeam(EntityUid shipEntity, EntityUid captain, string name = "", bool silent = false)
    {
        string shipName = GetName(shipEntity);
        string teamName = !IsValidName(name) ? GenerateTeamName() : name;
        ShipEventFaction team = new ShipEventFaction(
            teamName,
            "/Textures/Theta/ShipEvent/ShipFactionIcon.rsi",
            shipEntity,
            captain);
        _teams.Add(team);
        _shipNames[shipEntity] = shipName;
		if(!silent)
		{
			Announce(Loc.GetString(
				"shipevent-team-add",
				("teamname", team.Name),
				("shipname", shipName)));
		}

        var marker = _entMan.AddComponent<ShipEventFactionMarkerComponent>(shipEntity);
        marker.Team = team;

        return team;
    }

    public EntityUid CreateShip()
    {
        Vector2i mapPos = (Vector2i)_random.NextVector2(MaxSpawnOffset);

        MapLoadOptions loadOptions = new MapLoadOptions
        {
            Rotation = _random.NextAngle(),
            Offset = mapPos,
            LoadMap = false
        };

        if (_mapSys.TryLoad(TargetMap, _random.Pick(ShipTypes), out var rootUids, loadOptions)) { return rootUids[0]; }

        return EntityUid.Invalid;
    }

    /// <summary>
    /// Deletes current team ship & members, and marks it for respawn
    /// </summary>
    /// <param name="team">Team to respawn</param>
    /// <param name="respawnReason">Message to show in announcement</param>
    /// <param name="silent">Whether to announce respawn</param>
    /// <param name="immediate">If this team should be respawned without delay</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on respawned one</param>
    private void RespawnTeam(ShipEventFaction team, string respawnReason = "", bool silent = false, bool immediate = false, bool killPoints = true)
    {
        if (!silent)
        {
            string message = Loc.GetString(
                "shipevent-team-respawn",
                ("teamname", team.Name),
                ("shipname", GetName(team.Ship)),
                ("respawnreason", respawnReason == "" ? Loc.GetString("shipevent-respawn-default") : respawnReason),
                ("respawntime", RespawnDelay / 60));
            Announce(message);
        }

        if (killPoints) { AddKillPoints(team); }
        team.Hits.Clear();

        string oldShipName = GetName(team.Ship);
        _shipNames.Remove(team.Ship);
        foreach(Role member in team.Members){ _entMan.DeleteEntity(team.GetMemberEntity(member)); }
        if (team.Ship != EntityUid.Invalid) { _entMan.DeleteEntity(team.Ship); }
        team.Ship = EntityUid.Invalid;

        if (immediate) { ImmediateRespawn(team, oldShipName); }
        else { team.ShouldRespawn = true; }
    }

    /// <summary>
    /// Immediately respawns team
    /// </summary>
    /// <param name="team">Team to respawn</param>
    /// <param name="oldShipName">Name of previous ship, so new one can have it too. New name will be empty if this is not specified</param>
    private void ImmediateRespawn(ShipEventFaction team, string oldShipName = "")
    {
        EntityUid newShip = CreateShip();
        if (!TryGetShipComponent(newShip, out GhostRoleMobSpawnerComponent spawnerComp)) { return; }

        ShipEventFactionMarkerComponent markComp = _entMan.AddComponent<ShipEventFactionMarkerComponent>(newShip);
        markComp.Team = team;

        if(oldShipName != ""){ SetName(newShip, oldShipName); }
        team.Ship = newShip;
        _shipNames[team.Ship] = GetName(newShip);

        if (TryGetShipComponent<GhostRoleMobSpawnerComponent>(newShip, out var shipSpawner))
        {
            List<IPlayerSession> sessions = new();
            foreach(Role member in team.Members)
            {
                IPlayerSession? session = GetSession(member.Mind);
                if (session != null) { sessions.Add(session); }
            }

            team.Members.Clear();
            foreach(IPlayerSession session in sessions) { spawnerComp.Take(session); }
        }
        else { Logger.Error($"Could not respawn team, as ship is missing spawner. Team: {team.Name}; Ship uid: {newShip};"); }

        team.Respawns++;
        team.ShouldRespawn = false;
    }

    /// <summary>
    /// Removes team entirely
    /// </summary>
    /// <param name="team">Team to remove</param>
    /// <param name="removeReason">Message to show in announcement</param>
    /// <param name="silent">Whether to announce removal</param>
    /// <param name="killPoints">Whether to add points to other teams for hits on removed one</param>
    private void RemoveTeam(ShipEventFaction team, string removeReason = "", bool silent = false, bool killPoints = true)
    {
        if (!silent)
		{
            string message = Loc.GetString(
				"shipevent-team-remove",
				("teamname", team.Name),
				("shipname", GetName(team.Ship)),
				("removereason", removeReason == "" ? Loc.GetString("shipevent-remove-default") : removeReason));
			Announce(message);
		}

        if(killPoints){ AddKillPoints(team); }

        _shipNames.Remove(team.Ship);
        foreach(Role member in team.Members){ _entMan.DeleteEntity(team.GetMemberEntity(member)); }
        if (team.Ship != EntityUid.Invalid) { _entMan.DeleteEntity(team.Ship); }
        _teams.Remove(team);
    }

    /// <summary>
    /// Adds points to other teams for destroying specified one.
    /// </summary>
    /// <param name="team">Team which is destroyed</param>
    private void AddKillPoints(ShipEventFaction team)
    {
        int totalHits = 0;
        foreach ((ShipEventFaction killerTeam, int hits) in team.Hits) { totalHits += hits; }

        foreach ((ShipEventFaction killerTeam, int hits) in team.Hits)
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

    /// <summary>
    /// Checks teams periodically
    /// </summary>
    /// <param name="deltaTime">How much time passed since last call</param>
	private void CheckTeams(float deltaTime)
	{
		foreach (ShipEventFaction team in _teams)
        {
            if (team.GetLivingMembersMinds().Count == 0 && team.Members.Any() && !team.ShouldRespawn)
			{
				RespawnTeam(
				    team,
                    Loc.GetString("shipevent-respawn-dead"));
                break;
            }

            if (!TryGetShipComponent<ShuttleConsoleComponent>(team.Ship, out var shuttleConsole) && !team.ShouldRespawn)
            {
                RespawnTeam(
                    team,
                    Loc.GetString("shipevent-respawn-tech"));
                break;
            }

            if (_shipNames.ContainsKey(team.Ship))
            {
                string shipName = GetName(team.Ship);
                if (shipName != _shipNames[team.Ship])
                {
                    string message = Loc.GetString(
                        "shipevent-team-shiprename",
                        ("teamname", team.Name),
                        ("oldname", _shipNames[team.Ship]),
                        ("newname", shipName));
                    Announce(message);
                    _shipNames[team.Ship] = shipName;
                }
            }

            if (!IsActive(team.Captain))
            {
                if (team.Members.Any())
                {
                    EntityUid newCap = team.GetMemberEntity(_random.Pick(team.Members));
                    TeamMessage(team, Loc.GetString("shipevent-team-captainchange", ("oldcap", GetName(team.Captain)), ("newcap", GetName(newCap))), color: Color.Magenta);
                    team.Captain = newCap;
                }
            }

            if (team.ShouldRespawn && team.TimeSinceRemoval > RespawnDelay)
            {
                TeamMessage(team, Loc.GetString("shipevent-team-respawnnow"), color: Color.Magenta);
                ImmediateRespawn(team);
            }

            if (team.BonusIntervalTimer > BonusInterval)
            {
                TeamMessage(team,
                    Loc.GetString("shipevent-team-bonusinterval",
                    ("time", BonusInterval / 60),
                    ("points", PointsPerInterval)),
                    color: Color.Magenta);
                team.Points += PointsPerInterval;
                team.BonusIntervalTimer -= BonusInterval;
            }

            team.TimeSinceRemoval += deltaTime;
            if(!team.ShouldRespawn) { team.BonusIntervalTimer += deltaTime; }
        }
	}

    private void Announce(string message)
	{
        _chatSys.DispatchGlobalAnnouncement(message, Loc.GetString("shipevent-announcement-title"));
	}

    private void TeamMessage(ShipEventFaction team, string message, ChatChannel chatChannel = ChatChannel.Local, Color? color = null)
    {
        foreach(Mind.Mind mind in team.GetLivingMembersMinds())
        {
            if (mind.Session != null)
            {
                _chatSys.SendSimpleMessage(message, mind.Session, chatChannel, color);
            }
        }
    }

	private string GenerateTeamName()
	{
		_lastTeamNumber += 1;
		return $"Team №{_lastTeamNumber}";
	}

    private EntityUid GetSpawnerShip(EntityUid shipEntity)
	{
		EntityUid? shipGrid = Transform(shipEntity).GridUid;
		if (shipGrid == null) { return EntityUid.Invalid; }

        return (EntityUid)shipGrid;
	}

    private string GetName(EntityUid entity)
    {
        if (_entMan.TryGetComponent<MetaDataComponent>(entity, out MetaDataComponent? metaComp)) { return metaComp.EntityName; }
        return String.Empty;
    }

    private void SetName(EntityUid entity, string name)
    {
        if (_entMan.TryGetComponent<MetaDataComponent>(entity, out MetaDataComponent? metaComp)) { metaComp.EntityName = name; }
        _idSys.QueueIdentityUpdate(entity);
    }

    private bool TryGetShipComponent<T>(EntityUid shipEntity, out T component) where T : IComponent
    {
        component = default!;
        foreach (T comp in _entMan.EntityQuery<T>())
        {
            if (Transform(comp.Owner).GridUid == shipEntity)
            {
                component = comp;
                return true;
            }
        }

        return false;
    }

    private IPlayerSession? GetSession(EntityUid entity)
    {
        if (_entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
        {
            if (mindComp.HasMind)
            {
                IPlayerSession? session = mindComp.Mind!.Session;
                if (session != null) { return session; }
            }
        }

        return null;
    }

    private IPlayerSession? GetSession(Mind.Mind mind)
    {
        IPlayerSession? session = mind.Session;
        if (session != null) { return session; }

        return null;
    }

    public bool TryGetTeam(EntityUid shipEntity, out ShipEventFaction team)
    {
        team = default!;
        foreach (ShipEventFaction fact in _teams)
        {
            if (fact.Ship == shipEntity)
            {
                team = fact;
                return true;
            }
        }

        return false;
    }

    public bool IsValidName(string name)
    {
        if (name == "") { return false; }
        foreach(ShipEventFaction team in _teams)
        {
            if (team.Name == name) { return false; }
        }

        return true;
    }

    public bool IsActive(EntityUid entity)
    {
        if (_entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
        {
            if (mindComp.HasMind)
            {
                if (!mindComp.Mind!.CharacterDeadPhysically && mindComp.Mind.Session != null) { return true; }
            }
        }

        return false;
    }
}
