using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent;

[NetSerializable, Serializable]
public sealed class BoundsOverlayInfoRequest : EntityEventArgs { }

[NetSerializable, Serializable]
public sealed class BoundsOverlayInfo : EntityEventArgs
{
    public MapId TargetMap;
    public Box2i Bounds;

    public BoundsOverlayInfo(MapId targetMap, Box2i bounds)
    {
        TargetMap = targetMap;
        Bounds = bounds;
    }
}
