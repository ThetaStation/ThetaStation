using System.Numerics;
using Content.Server.Theta.ShipEvent.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipEventWormholeAnomalyComponent : Component
{
    public EntityUid? BoundWormhole;

    [Access(typeof(ShipEventFactionSystem))]
    public Vector2 OriginalPosition;
}

[RegisterComponent]
public sealed partial class ShipEventWormholeSpawnerComponent : Component
{
    [DataField("wormholeProtoId", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WormholeProtoId;

    [DataField("minDistance")]
    public int MinDistance = 100;

    [DataField("maxDistance")]
    public int MaxDistance = 1000;
}