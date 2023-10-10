using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent;

[NetSerializable, Serializable]
public sealed class BoundsOverlayInfoRequest : EntityEventArgs { }

[NetSerializable, Serializable]
public sealed class BoundsOverlayInfo : EntityEventArgs
{
    public MapId TargetMap;
    public Box2 Bounds;

    public BoundsOverlayInfo(MapId targetMap, Box2 bounds)
    {
        TargetMap = targetMap;
        Bounds = bounds;
    }
}

//sent from shuttle console when user presses stealth activation button
[Serializable, NetSerializable]
public sealed class ShipEventToggleStealthMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class ShipEventRequestStealthStatusMessage : BoundUserInterfaceMessage { }

//sent to shuttle console in response to stealth status request
[Serializable, NetSerializable]
public sealed class ShipEventStealthStatusMessage : BoundUserInterfaceState
{
    public bool StealthReady;

    public ShipEventStealthStatusMessage(bool ready)
    {
        StealthReady = ready;
    }
}
