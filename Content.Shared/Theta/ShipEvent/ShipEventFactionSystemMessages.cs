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

[Serializable, NetSerializable]
public sealed class ShipEventToggleStealthMessage : BoundUserInterfaceMessage
{
    public bool Show;
}
