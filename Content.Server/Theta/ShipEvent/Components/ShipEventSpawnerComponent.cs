using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Theta.ShipEvent.Components;

/// <summary>
/// Marker for shipevent spawners
/// </summary>
[RegisterComponent]
public sealed partial class ShipEventSpawnerComponent : Component
{
    [CanBeNull]
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("prototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? Prototype;
}
