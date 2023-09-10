using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Explosion.Components;

[RegisterComponent]
public sealed partial class ShuttlePickableComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("randomPrototypes", required: true, customTypeSerializer:typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> Prototypes = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("targetTeleportId", required: true)]
    public string TargetTeleportId = string.Empty;
}
