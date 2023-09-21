using System.Linq;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;

namespace Content.Shared.Roles.Theta;

public sealed class PlayerFactionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    public List<AntagonistRoleComponent> GetActiveMembers(PlayerFaction faction)
    {
        var list = new List<AntagonistRoleComponent>();
        foreach (var member in faction.Members)
        {
            if(_mindSystem.TryGetSession(member.Owner, out var session))
                list.Add(member);
        }
        return list;
    }

    public AntagonistRoleComponent? TryGetRoleByEntity(PlayerFaction faction, EntityUid member)
    {
        return faction.Members.FirstOrDefault(role => role.Owner == member);
    }

    public List<EntityUid> GetLivingMembersEntities(PlayerFaction faction)
    {
        List<EntityUid> living = new();
        foreach (var member in faction.Members)
        {
            if(!TryComp<MindComponent>(member.Owner, out _))
                continue;
            if (!_mobStateSystem.IsDead(member.Owner))
                living.Add(member.Owner);
        }
        return living;
    }

    public List<MindComponent> GetLivingMembersMinds(PlayerFaction faction)
    {
        List<MindComponent> living = new();
        foreach (var member in faction.Members)
        {
            if(!TryComp<MindComponent>(member.Owner, out var mind))
                continue;
            if (!_mobStateSystem.IsDead(member.Owner))
                living.Add(mind);
        }

        return living;
    }

    public List<string> GetMemberUserNames(PlayerFaction faction)
    {
        List<string> names = new();
        foreach (var member in faction.Members)
        {
            if(_mindSystem.TryGetSession(member.Owner, out var session))
                names.Add(session.ConnectedClient.UserName);
        }

        return names;
    }

    /// <summary>
    /// Returns dictionary of ckeys, associated with member roles (logged-in members only)
    /// </summary>
    public Dictionary<string, AntagonistRoleComponent> GetMembersByUserNames(PlayerFaction faction)
    {
        Dictionary<string, AntagonistRoleComponent> pairs = new();
        foreach (var member in faction.Members)
        {
            if(_mindSystem.TryGetSession(member.Owner, out var session))
                pairs[session.ConnectedClient.UserName] = member;
        }

        return pairs;
    }
}
