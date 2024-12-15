using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class TeamViewBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<TeamViewTeamState> Teams;

    public TeamViewBoundUserInterfaceState(List<TeamViewTeamState> teams)
    {
        Teams = teams;
    }
}

[Serializable, NetSerializable]
public sealed class TeamViewTeamState
{
    public string? Name;
    public Color Color;
    public string? ShipName;
    public string? AliveCrewCount;
    public int Points;
}

[Serializable, NetSerializable]
public enum TeamViewUiKey
{
    Key
}
