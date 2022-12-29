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
using Content.Shared.Toggleable;
using Content.Shared.Theta.ShipEvent;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class ShipEventFactionSystem : EntitySystem
{
	[Dependency] private readonly IEntityManager entMan = default!;
    [Dependency] private readonly ChatSystem chatSystem = default!;
    [Dependency] private readonly IdentitySystem identitySystem = default!;
    [Dependency] private readonly ActionsSystem actionsSystem = default!;
    [Dependency] private readonly UserInterfaceSystem uiSystem = default!;

    private const float TeamCheckInterval = 5;
    private float TeamCheckTimer = 0;

	private int LastTeamNumber;

	/// <summary>
	/// Dictionary with all the teams, where spawner uid is the key, and faction is the value
	/// </summary>
	public Dictionary<EntityUid, PlayerFaction> Teams => _teams;

	private Dictionary<EntityUid, PlayerFaction> _teams = new();

    private Dictionary<EntityUid, string> shipNames = new();

    public override void Initialize()
	{
		base.Initialize();
		SubscribeLocalEvent<ShipEventFactionComponent, GhostRoleSpawnerUsedEvent>(OnSpawn);
        SubscribeLocalEvent<ShipEventFactionViewComponent, ToggleActionEvent>(OnView);
        SubscribeLocalEvent<ShipEventFactionViewComponent, ComponentInit>(OnViewInit);
    }

	public override void Update(float frametime)
    {
        if (TeamCheckTimer < TeamCheckInterval)
        {
            TeamCheckTimer += frametime;
            return;
        }
        TeamCheckTimer = 0;
        CheckTeams();
    }

	private void OnSpawn(EntityUid entity, ShipEventFactionComponent component, GhostRoleSpawnerUsedEvent args)
    {
        EntityUid shipEntity = GetSpawnerShip(args.Spawner);
        if (shipEntity == EntityUid.Invalid) { return; }
		if (!_teams.ContainsKey(shipEntity)){ CreateTeam(shipEntity); }
		AddToTeam(args.Spawned, shipEntity);
        entMan.GetComponent<GhostRoleMobSpawnerComponent>(args.Spawner).SetCurrentTakeovers(_teams[shipEntity].GetLivingMembers().Count);
    }

    private void OnView(EntityUid entity, ShipEventFactionViewComponent component, ToggleActionEvent args)
    {
        string result = $"\n{Loc.GetString("shipevent-teamview-heading")}";
        result += $"\n{Loc.GetString("shipevent-teamview-heading2")}";
        foreach (EntityUid shipEntity in _teams.Keys)
        {
            result += $"\n'{_teams[shipEntity].Name}' - '{shipNames[shipEntity]}' - {_teams[shipEntity].GetLivingMembers().Count}";
        }

        Enum uiKey = TeamViewUiKey.Key;
        IPlayerSession? session = GetEntitySession(entity);
        if (session == null) { return; }

        if (!uiSystem.IsUiOpen(entity, uiKey))
        {
            if (uiSystem.TryGetUi(entity, uiKey, out var bui))
            {
                uiSystem.OpenUi(bui, session);
                uiSystem.SetUiState(bui, new TeamViewBoundUserInterfaceState(result));
            }
        }
    }

    private void OnViewInit(EntityUid entity, ShipEventFactionViewComponent view, ComponentInit args)
    {
        if (entMan.TryGetComponent<ActionsComponent>(entity, out var actComp)) { actionsSystem.AddAction(entity, view.ToggleAction, null, actComp); }
    }

    private void AddToTeam(EntityUid entity, EntityUid shipEntity)
	{
		if (entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
		{
			if (!mindComp.HasMind) { return; }
			Role shipEventRole = new ShipEventRole(mindComp.Mind!);
			mindComp.Mind!.AddRole(shipEventRole);
			_teams[shipEntity].AddMember(shipEventRole);
            entMan.GetEntityQuery<MetaDataComponent>().GetComponent(entity).EntityName += $" ({_teams[shipEntity].Name})";
            identitySystem.QueueIdentityUpdate(entity);
        }
	}

	private void CreateTeam(EntityUid shipEntity, bool silent = false)
    {
        string shipName = GetShipName(shipEntity);
		_teams[shipEntity] = new PlayerFaction(GenerateTeamName(), "/Textures/Theta/ShipEvent/ShipFactionIcon.rsi");
        shipNames[shipEntity] = shipName;
		if(!silent)
		{
			Announce(Loc.GetString(
				"shipevent-team-add",
				("teamname", _teams[shipEntity].Name),
				("shipname", shipName)));
		}
	}

	private void RemoveTeam(EntityUid shipEntity, bool silent = false, string removeReason = "")
    {
        if (!silent)
		{
			string _removeReason = Loc.GetString("shipevent-remove-default");
			if (removeReason != "") { _removeReason = removeReason; }
			string message = Loc.GetString(
				"shipevent-team-remove",
				("teamname", _teams[shipEntity].Name),
				("shipname", GetShipName(shipEntity)),
				("removereason", _removeReason));
			Announce(message);
		}

        foreach(Role member in _teams[shipEntity].Members){ entMan.DeleteEntity(_teams[shipEntity].GetMemberEntity(member)); }
        if (shipEntity != EntityUid.Invalid) { entMan.DeleteEntity(shipEntity); }
        _teams.Remove(shipEntity);
    }

	private void CheckTeams()
	{
		foreach (EntityUid shipEntity in _teams.Keys)
        {
            PlayerFaction faction = _teams[shipEntity];
            string shipName = GetShipName(shipEntity);
            if (shipName != shipNames[shipEntity])
            {
                string message = Loc.GetString(
                    "shipevent-team-shiprename",
                    ("teamname", _teams[shipEntity].Name),
                    ("oldname", shipNames[shipEntity]),
                    ("newname", shipName));
                Announce(message);
                shipNames[shipEntity] = shipName;
            }
			if (faction.GetLivingMembers().Count == 0 && faction.Members.Any())
			{
				RemoveTeam(
				    shipEntity,
				    false,
				    Loc.GetString("shipevent-remove-dead"));
			}
            else if (!HasShuttleConsole(shipEntity))
            {
                RemoveTeam(
                    shipEntity,
                    false,
                    Loc.GetString("shipevent-remove-tech"));
            }
		}
	}

    private void Announce(string message)
	{
        chatSystem.DispatchGlobalAnnouncement(message, Loc.GetString("shipevent-announcement-title"));
	}

	private string GenerateTeamName()
	{
		LastTeamNumber += 1;
		return $"Team №{LastTeamNumber}";
	}

	private EntityUid GetSpawnerShip(EntityUid shipEntity)
	{
		EntityUid? shipGrid = Transform(shipEntity).GridUid;
		if (shipGrid == null) { return EntityUid.Invalid; }

        return (EntityUid)shipGrid;
	}

    private string GetShipName(EntityUid shipEntity)
    {
       return entMan.GetEntityQuery<MetaDataComponent>().GetComponent(shipEntity).EntityName;
    }

    private bool HasShuttleConsole(EntityUid shipEntity)
    {
        foreach (ShuttleConsoleComponent console in entMan.EntityQuery<ShuttleConsoleComponent>())
        {
            if (Transform(console.Owner).GridUid == shipEntity) { return true; }
        }
        return false;
    }

    private IPlayerSession? GetEntitySession(EntityUid entity)
    {
        if (entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
        {
            if (mindComp.HasMind)
            {
                IPlayerSession? session = mindComp.Mind!.Session;
                if (session != null) { return session; }
            }
        }

        return null;
    }
}
