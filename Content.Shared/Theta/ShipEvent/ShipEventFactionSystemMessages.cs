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

[NetSerializable, Serializable]
public sealed class LootboxInfoRequest : EntityEventArgs { }

[NetSerializable, Serializable]
public sealed class LootboxInfo : EntityEventArgs
{
    public List<EntityUid> Entities;
    public List<Box2> Bounds;
    public List<float> Lifetime;

    public LootboxInfo(List<EntityUid> ents, List<Box2> bounds, List<float> lifetime)
    {
        Entities = ents;
        Bounds = bounds;
        Lifetime = lifetime;
    }
}
