using Robust.Shared.Utility;

namespace Content.Shared.Roles.Theta;

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
    /// Members of the faction as component of mind
    /// </summary>
    public List<AntagonistRoleComponent> Members { get; }

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
}
