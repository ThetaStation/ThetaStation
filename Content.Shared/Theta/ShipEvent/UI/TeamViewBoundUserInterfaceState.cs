using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class TeamViewBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<TeamInterfaceState> Teams;
    public readonly List<string> Modifiers;

    public TeamViewBoundUserInterfaceState(List<TeamInterfaceState> teams, List<string> modifiers)
    {
        Teams = teams;
        Modifiers = modifiers;
    }
}

[Serializable, NetSerializable]
public sealed class TeamViewTeamState
{
    public string? Name;
    public Color Color;
    public string? Fleet;
    public string? ShipName;
    public string? AliveCrewCount;
    public int Points;
}

[Serializable, NetSerializable]
public enum TeamViewUiKey
{
    Key
}
