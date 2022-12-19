using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.ShipEvent.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server.ShipEvent.Systems;

public sealed class ShipEventFactionSystem : EntitySystem
{
	[Dependency] private readonly IEntityManager entMan = default!;
	[Dependency] private readonly ChatSystem chatSystem = default!;

	private int LastTeamNumber;

	/// <summary>
	/// Dictionary with all the teams, where spawner uid is the key, and faction is the value
	/// </summary>
	public Dictionary<EntityUid, PlayerFaction> Teams => teams;

	private Dictionary<EntityUid, PlayerFaction> teams = new();

	public override void Initialize()
	{
		base.Initialize();
		SubscribeLocalEvent<ShipEventFactionComponent, GhostRoleSpawnerUsedEvent>(OnSpawn);
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

	private void AddToTeam(EntityUid entity, EntityUid spawnerEntity)
	{
		if (entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
		{
			if (!mindComp.HasMind) { return; }
			Role shipEventRole = new ShipEventRole(mindComp.Mind!);
			mindComp.Mind!.AddRole(shipEventRole);
			teams[spawnerEntity].AddMember(shipEventRole);
            mindComp.Mind.CharacterName += $"(Team №{teams[spawnerEntity].Name})";
        }
	}

	private void CreateTeam(EntityUid spawnerEntity, bool silent = false)
	{
		(EntityUid shipGrid, string shipName) = GetShipData(spawnerEntity);
		teams[spawnerEntity] = new PlayerFaction(GenerateTeamName(), "/Textures/Theta/ShipEvent/ShipFactionIcon.rsi");
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
			if (faction.GetLivingMembers().Count == 0 && faction.Members.Any())
			{
				RemoveTeam(
				spawnerEntity,
				false,
				Loc.GetString("shipevent-remove-dead"));
			}
            else if (!HasShuttleConsole(spawnerEntity))
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

    private bool HasShuttleConsole(EntityUid spawnerEntity)
    {
        (EntityUid shipGrid, string shipName) = GetShipData(spawnerEntity);
        foreach (ShuttleConsoleComponent console in entMan.EntityQuery<ShuttleConsoleComponent>())
        {
            if (Transform(console.Owner).GridUid == shipGrid) { return true; }
        }
        return false;
    }
}
