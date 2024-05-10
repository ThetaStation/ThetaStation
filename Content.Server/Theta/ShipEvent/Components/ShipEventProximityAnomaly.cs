using Content.Server.Theta.ShipEvent.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Theta.ShipEvent.Components;

/// <summary>
/// Upon ship entering into it's range, starts spawning specified prototype onto it's grid and applies anomaly overlay for every crewmember.
/// </summary>
[RegisterComponent]
public sealed partial class ShipEventProximityAnomalyComponent : Component
{
    [DataField("range", required: true)]
    public int Range;

    [DataField("toSpawn", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ToSpawn;

    [Access(new Type[] { typeof(ShipEventFactionSystem) })]
    public HashSet<EntityUid> TrackedUids = new();
}