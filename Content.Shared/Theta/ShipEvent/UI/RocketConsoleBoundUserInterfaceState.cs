using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent;

[Serializable, NetSerializable]
public enum RocketConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class RocketConsoleLaunchMessage : BoundUserInterfaceMessage
{
    public List<Vector2> Waypoints = new();
}
