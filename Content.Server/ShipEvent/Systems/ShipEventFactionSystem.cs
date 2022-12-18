using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind.Components;
using Content.Server.Roles;
using Content.Server.ShipEvent.Components;
using Content.Server.Shuttles.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server.ShipEvent.Systems;

public sealed class ShipEventFactionSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager entMan = default!;
    [Dependency] private readonly ChatSystem chatSystem = default!;

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

    public override void Update(float frametime)
    {
        CheckTeams();
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
            if (!mindComp.HasMind) { return; }
            Role shipEventRole = new ShipEventRole(mindComp.Mind!);
            mindComp.Mind!.AddRole(shipEventRole);
            Teams![spawnerEntity].AddMember(shipEventRole);
        }
    }

    private void CreateTeam(EntityUid spawnerEntity, bool silent = false)
    {
        (EntityUid shipGrid, string shipName) = GetShipData(spawnerEntity);
        Teams![spawnerEntity] = new PlayerFaction(GenerateTeamName(), "/Textures/Theta/ShipEvent/ShipFactionIcon.rsi");
        if(!silent)
        {
            Announce(Loc.GetString(
                "shipevent-team-add",
                ("teamname", Teams![spawnerEntity].Name),
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
                ("teamname", Teams![spawnerEntity].Name),
                ("shipname", shipName),
                ("removereason", _removeReason));
            Announce(message);
        }

        Teams!.Remove(spawnerEntity);
        if (shipGrid != null)
        {
            entMan.DeleteEntity((EntityUid)shipGrid);
            entMan.DeleteEntity(spawnerEntity);
        }
    }

    private void CheckTeams()
    {
        foreach (EntityUid spawnerEntity in Teams!.Keys)
        {
            PlayerFaction faction = Teams![spawnerEntity];
            if (faction.GetLivingMembers().Count == 0 && faction.Members.Any()) {
                RemoveTeam(
                spawnerEntity,
                false,
                Loc.GetString("shipevent-remove-dead"));
            }
        }
    }

    private void Announce(string message)
    {
        chatSystem.DispatchGlobalAnnouncement(message, Loc.GetString("shipevent-announcement-title"));
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

    private (EntityUid, string) GetShipData(EntityUid spawnerEntity)
    {
        TransformComponent transform = entMan.GetComponent<TransformComponent>(spawnerEntity);
        EntityUid? shipGrid = transform.GridUid;
        if (shipGrid == null) { return (EntityUid.Invalid, ""); }
        string shipName = entMan.GetEntityQuery<MetaDataComponent>().GetComponent((EntityUid)shipGrid).EntityName;

        return ((EntityUid)shipGrid, shipName);
    }
}
