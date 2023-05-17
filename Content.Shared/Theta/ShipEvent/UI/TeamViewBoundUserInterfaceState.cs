using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class TeamViewBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ShipTeamForTeamViewState> Teams;

    public TeamViewBoundUserInterfaceState(List<ShipTeamForTeamViewState> teams)
    {
        Teams = teams;
    }
}

[Serializable, NetSerializable]
public sealed class ShipTeamForTeamViewState
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
