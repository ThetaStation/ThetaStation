using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class TeamInterfaceState
{
    public string Name = string.Empty;
    public Color Color;
    public string? Fleet;
    public string? Captain;
    public int Points;
    public int Members;
    public int MaxMembers;
    public bool HasPassword;
}
