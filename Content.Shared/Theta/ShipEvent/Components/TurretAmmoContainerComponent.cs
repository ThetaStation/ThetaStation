using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Theta.ShipEvent.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TurretAmmoContainerComponent : Component
{
    [DataField("ammoPrototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string AmmoPrototype;

    [DataField("ammoCount", required: true)]
    public int MaxAmmoCount;

    [AutoNetworkedField]
    public int AmmoCount;
}
