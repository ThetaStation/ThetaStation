using System.Linq;
using Content.Shared.Mind;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

public abstract class PlayerFaction
{
    /// <summary>
    /// Name of the faction
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Icon of the faction
    /// </summary>
    public SpriteSpecifier? Icon { get; }

    /// <summary>
    /// Members of the faction
    /// </summary>
    public List<AntagonistRoleComponent> Members { get; }

    /// <summary>
    /// List of members with attached clients
    /// </summary>
    public List<AntagonistRoleComponent> ActiveMembers => Members.Where(r => r.Mind.Session != null).ToList();

    public List<string> ActiveMemberUsernames => ActiveMembers.Select(r => r.Mind.Session!.ConnectedClient.UserName).ToList();

    public PlayerFaction(string name, string iconPath = "")
    {
        Name = name;
        if(iconPath != "")
            Icon = new SpriteSpecifier.Texture(new ResPath(iconPath));
        Members = new List<AntagonistRoleComponent>();
    }

    public void AddMember(AntagonistRoleComponent member)
    {
        if (Members.Contains(member))
            return;

        member.Faction = this;
        Members.Add(member);
    }

    public void RemoveMember(AntagonistRoleComponent member)
    {
        if (!Members.Contains(member))
            return;

        member.Faction = null;
        Members.Remove(member);
    }

    public EntityUid GetMemberEntity(AntagonistRoleComponent member)
    {
        if (!Members.Contains(member))
            return EntityUid.Invalid;

        EntityUid? entity = member.Mind.OwnedEntity;
        if (entity != null)
            return (EntityUid) entity;

        return EntityUid.Invalid;
    }

    public AntagonistRoleComponent? TryGetRoleByEntity(EntityUid member)
    {
        foreach (var role in Members)
        {
            if (role.Mind.OwnedEntity == member)
                return role;
        }

        return null;
    }

    public ICommonSession? GetMemberSession(AntagonistRoleComponent member)
    {
        return member.Mind.Session;
    }

    public List<EntityUid> GetLivingMembersEntities()
    {
        List<EntityUid> living = new();
        var mindSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedMindSystem>();
        foreach (AntagonistRoleComponent member in Members)
        {
            if (!mindSystem.IsCharacterDeadPhysically(member.Owner))
                living.Add((EntityUid)member.Owner);
        }

        return living;
    }

    public List<Mind.Mind> GetLivingMembersMinds()
    {
        List<Mind.Mind> living = new();
        var mindSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedMindSystem>();
        foreach (AntagonistRoleComponent member in Members)
        {
            if (!mindSystem.IsCharacterDeadPhysically(member.Owner))
                living.Add(member.Mind);
        }

        return living;
    }

    /// <summary>
    /// Returns list of ckeys in this faction (logged-in members only)
    /// </summary>
    public List<string> GetMemberUserNames()
    {
        List<string> names = new();
        var mindSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedMindSystem>();
        foreach (AntagonistRoleComponent member in Members)
        {
            if (mindSystem.TryGetSession(member.Owner, out var session))
                names.Add(session.ConnectedClient.UserName);
        }

        return names;
    }

    /// <summary>
    /// Returns dictionary of ckeys, associated with member roles (logged-in members only)
    /// </summary>
    public Dictionary<string, AntagonistRoleComponent> GetMembersByUserNames()
    {
        Dictionary<string, AntagonistRoleComponent> pairs = new();
        var mindSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SharedMindSystem>();
        foreach (AntagonistRoleComponent member in Members)
        {
            if (mindSystem.TryGetSession(member.Owner, out var session))
                pairs[session.ConnectedClient.UserName] = member;
        }

        return pairs;
    }
}
