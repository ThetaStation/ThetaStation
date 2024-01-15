using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class ShieldConsoleBoundsUserInterfaceState : BoundUserInterfaceState
{
    public readonly float MaxRange;
    public NetCoordinates? Coordinates;
    public Angle? Angle;
    public ShieldInterfaceState Shield;

    public ShieldConsoleBoundsUserInterfaceState(float maxRange, NetCoordinates? coordinates, Angle? angle, ShieldInterfaceState shield)
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Shield = shield;
    }
}

[Serializable, NetSerializable]
public enum CircularShieldConsoleUiKey
{
    Key
}

