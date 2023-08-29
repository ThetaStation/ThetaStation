using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Explosion.Components;

[RegisterComponent]
public sealed class ShuttlePickableComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Proto = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField("targetTeleportId", required: true)]
    public string TargetTeleportId = string.Empty;
}
