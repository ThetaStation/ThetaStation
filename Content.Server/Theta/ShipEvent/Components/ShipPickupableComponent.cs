using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipPickupableComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("prototypes", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> EntsToSpawn = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("targetBeaconId", required: true)]
    public string TargetBeaconId = string.Empty;
}
