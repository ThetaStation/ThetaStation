using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class TeamViewBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<TeamState> Teams;

    public TeamViewBoundUserInterfaceState(List<TeamState> teams)
    {
        Teams = teams;
    }
}

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class TeamState
{
    public string? Name;
    public string? Color;
    public string? ShipName;
    public string? AliveCrewCount;
    public int Points;
}

[Serializable, NetSerializable]
public enum TeamViewUiKey
{
    Key
}
