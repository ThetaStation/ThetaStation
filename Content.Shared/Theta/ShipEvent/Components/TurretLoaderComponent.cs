using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TurretLoaderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundTurretUid;

    /// <summary>
    /// Slot containing ammo container
    /// </summary>
    public ItemSlot? ContainerSlot;

    /// <summary>
    /// Played when container with invalid ammo type is inserted
    /// </summary>
    [DataField("invalidAmmoSound")]
    public SoundSpecifier? InvalidAmmoTypeSound;
}

[Serializable, NetSerializable]
public sealed class TurretLoaderState : ComponentState
{
    public NetEntity? BoundTurret;

    public string? ContainerSlotID;

    public TurretLoaderState(NetEntity? boundTurret, string? containerSlotId)
    {
        BoundTurret = boundTurret;
        ContainerSlotID = containerSlotId;
    }
}
