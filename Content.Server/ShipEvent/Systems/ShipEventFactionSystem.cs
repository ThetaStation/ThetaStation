using System.Linq;
using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Ghost.Roles.Events;
using Content.Server.IdentityManagement;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.ShipEvent.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Actions;
using Content.Shared.Toggleable;

namespace Content.Server.ShipEvent.Systems;

public sealed class ShipEventFactionSystem : EntitySystem
{
	[Dependency] private readonly IEntityManager entMan = default!;
	[Dependency] private readonly ChatSystem chatSystem = default!;
    [Dependency] private readonly IChatManager chatMan = default!;

	private int LastTeamNumber;

	/// <summary>
	/// Dictionary with all the teams, where spawner uid is the key, and faction is the value
	/// </summary>
	public Dictionary<EntityUid, PlayerFaction> Teams => teams;

	private Dictionary<EntityUid, PlayerFaction> teams = new();

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
		CheckTeams();
	}

	private void OnSpawn(EntityUid entity, ShipEventFactionComponent component, GhostRoleSpawnerUsedEvent args)
	{
		if (!teams.ContainsKey(args.Spawner)){ CreateTeam(args.Spawner); }
		AddToTeam(args.Spawned, args.Spawner);
	}

    private void OnView(EntityUid entity, ShipEventFactionViewComponent component, ToggleActionEvent args)
    {
        string result = $"\n{Loc.GetString("shipevent-teamview-heading")}";
        result += $"\n{Loc.GetString("shipevent-teamview-heading2")}";
        foreach (EntityUid spawnerEntity in teams.Keys)
        {
            result += $"\n'{teams[spawnerEntity].Name}' - '{shipNames[spawnerEntity]}' - {teams[spawnerEntity].GetLivingMembers().Count}";
        }

        if (entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
        {
            if (mindComp.Mind!.TryGetSession(out var session)) { chatMan.DispatchServerMessage(session, result); }
        }
    }

    private void OnViewInit(EntityUid entity, ShipEventFactionViewComponent view, ComponentInit args)
    {
        if (entMan.TryGetComponent<ActionsComponent>(entity, out var actComp))
        {
            if (entMan.TrySystem<ActionsSystem>(out var actSys)) { actSys.AddAction(entity, view.ToggleAction, null, actComp); }
        }
    }

    private void AddToTeam(EntityUid entity, EntityUid spawnerEntity)
	{
		if (entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
		{
			if (!mindComp.HasMind) { return; }
			Role shipEventRole = new ShipEventRole(mindComp.Mind!);
			mindComp.Mind!.AddRole(shipEventRole);
			teams[spawnerEntity].AddMember(shipEventRole);
            entMan.GetEntityQuery<MetaDataComponent>().GetComponent(entity).EntityName += $" ({teams[spawnerEntity].Name})";
            if (entMan.TrySystem<IdentitySystem>(out IdentitySystem? system)) { system.QueueIdentityUpdate(entity); }
        }
	}

	private void CreateTeam(EntityUid spawnerEntity, bool silent = false)
	{
		(EntityUid shipGrid, string shipName) = GetShipData(spawnerEntity);
		teams[spawnerEntity] = new PlayerFaction(GenerateTeamName(), "/Textures/Theta/ShipEvent/ShipFactionIcon.rsi");
        shipNames[spawnerEntity] = shipName;
		if(!silent)
		{
			Announce(Loc.GetString(
				"shipevent-team-add",
				("teamname", teams[spawnerEntity].Name),
				("shipname", shipName)));
		}
	}

	private void RemoveTeam(EntityUid spawnerEntity, bool silent = false, string removeReason = "")
	{
		(EntityUid shipGrid, string shipName) = GetShipData(spawnerEntity);

		if (!silent)
		{
			string _removeReason = Loc.GetString("shipevent-remove-default");
			if (removeReason != "") { _removeReason = removeReason; }
			string message = Loc.GetString(
				"shipevent-team-remove",
				("teamname", teams[spawnerEntity].Name),
				("shipname", shipName),
				("removereason", _removeReason));
			Announce(message);
		}

		teams.Remove(spawnerEntity);
		if (shipGrid != EntityUid.Invalid)
		{
			entMan.DeleteEntity(shipGrid);
			entMan.DeleteEntity(spawnerEntity);
		}
	}

	private void CheckTeams()
	{
		foreach (EntityUid spawnerEntity in teams.Keys)
		{
			PlayerFaction faction = teams[spawnerEntity];
            (EntityUid shipGrid, string shipName) = GetShipData(spawnerEntity);
            if (shipName != shipNames[spawnerEntity])
            {
                string message = Loc.GetString(
                    "shipevent-team-shiprename",
                    ("teamname", teams[spawnerEntity].Name),
                    ("oldname", shipNames[spawnerEntity]),
                    ("newname", shipName));
                Announce(message);
                shipNames[spawnerEntity] = shipName;
            }
			if (faction.GetLivingMembers().Count == 0 && faction.Members.Any())
			{
				RemoveTeam(
				    spawnerEntity,
				    false,
				    Loc.GetString("shipevent-remove-dead"));
			}
            else if (!HasShuttleConsole(shipGrid))
            {
                RemoveTeam(
                    spawnerEntity,
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

	private (EntityUid, string) GetShipData(EntityUid spawnerEntity)
	{
		EntityUid? shipGrid = Transform(spawnerEntity).GridUid;
		if (shipGrid == null) { return (EntityUid.Invalid, ""); }
		string shipName = entMan.GetEntityQuery<MetaDataComponent>().GetComponent((EntityUid)shipGrid).EntityName;

		return ((EntityUid)shipGrid, shipName);
	}

    private bool HasShuttleConsole(EntityUid shipEntity)
    {
        foreach (ShuttleConsoleComponent console in entMan.EntityQuery<ShuttleConsoleComponent>())
        {
            if (Transform(console.Owner).GridUid == shipEntity) { return true; }
        }
        return false;
    }
}
