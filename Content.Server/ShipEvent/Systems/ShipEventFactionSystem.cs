using System.Linq;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.ShipEvent.Components;

namespace Content.Server.ShipEvent.Systems;

public sealed class ShipEventFactionSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager entMan = default!;

    private int LastTeamNumber = 0;

    /// <summary>
    /// Dictionary with all the teams, where spawner uid is the key, and faction is the value
    /// </summary>
    public Dictionary<EntityUid, PlayerFaction>? Teams
    {
        get;
        private set;
    }
    public override void Initialize()
    {
        base.Initialize();
        Teams = new Dictionary<EntityUid, PlayerFaction>();
        SubscribeLocalEvent<ShipEventFactionComponent, GhostRoleSpawnerUsedEvent>(OnSpawn);
    }

    private void OnSpawn(EntityUid entity, ShipEventFactionComponent component, GhostRoleSpawnerUsedEvent args)
    {
        if (Teams!.ContainsKey(args.Spawner))
        {
            AddToTeam(args.Spawned, args.Spawner);
        }
        else
        {
            CreateTeam(args.Spawner);
            AddToTeam(args.Spawned, args.Spawner);
        }
    }

    private void AddToTeam(EntityUid entity, EntityUid spawnerEntity)
    {
        List<Role> roles = new();
        if (entMan.TryGetComponent<MindComponent>(entity, out var mindComp))
        {
            if (mindComp.HasMind) { roles = (List<Role>)mindComp.Mind!.AllRoles; }
        }

        foreach (Role role in roles)
        {
            if (role.Name == "Ship Team") { role.Faction = Teams![spawnerEntity]; }
        }
    }

    private void CreateTeam(EntityUid spawnerEntity, bool silent = false)
    {
        Teams![spawnerEntity] = new PlayerFaction(GenerateTeamName(), "/Textures/Theta/ShipEvent/ShipFactionIcon.rsi");
    }

    private string GenerateTeamName()
    {
        var taken = GetTakenNames();
        while (true)
        {
            LastTeamNumber += 1;
            string name = "Team №" + LastTeamNumber.ToString();
            if (!taken.Contains(name)) { return name; }
        }
    }

    private IEnumerable<string> GetTakenNames()
    {
        List<string> taken = new();
        foreach (EntityUid spawnerEntity in Teams!.Keys) { taken.Add(Teams![spawnerEntity].Name); }
        return taken;
    }

    private void RemoveTeam(EntityUid spawnerEntity, bool silent = false)
    {
        Teams!.Remove(spawnerEntity);
        TransformComponent transform = entMan.GetComponent<TransformComponent>(spawnerEntity);
        if (transform.GridUid != null)
        {
            entMan.DeleteEntity((EntityUid)transform.GridUid);
            entMan.DeleteEntity(spawnerEntity);
        }
    }

    private void CheckTeams()
    {
        foreach (EntityUid spawnerEntity in Teams!.Keys)
        {
            if (Teams![spawnerEntity].GetLivingMembers().Count == 0) { RemoveTeam(spawnerEntity); }
        }
    }
}
