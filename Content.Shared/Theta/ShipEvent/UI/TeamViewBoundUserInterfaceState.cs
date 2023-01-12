using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class TeamViewBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly string Text;

    public TeamViewBoundUserInterfaceState(string text) { Text = text; }
}

[Serializable, NetSerializable]
public enum TeamViewUiKey
{
    Key
}
