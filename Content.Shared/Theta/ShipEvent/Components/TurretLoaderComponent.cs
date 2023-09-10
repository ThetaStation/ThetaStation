using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TurretLoaderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] 
    public List<EntityUid> BoundTurret = new();
    
    /// <summary>
    /// Slot containing ammo container
    /// </summary>
    public ItemSlot? ContainerSlot;

    /// <summary>
    /// Entity container of inserted ammo container
    /// </summary>
    public Container? AmmoContainer;

    /// <summary>
    /// Maximum capacity of currently loaded container
    /// </summary>
    public int MaxContainerCapacity;

    /// <summary>
    /// Played when container with invalid ammo type is inserted
    /// </summary>
    [DataField("invalidAmmoSound")]
    public SoundSpecifier? InvalidAmmoTypeSound;
}

[Serializable, NetSerializable]
public sealed class TurretLoaderState : ComponentState
{
    public List<EntityUid> BoundTurret = new();

    public int MaxContainerCapacity;

    public string? ContainerSlotID;

    public string? ContainerID;

    public TurretLoaderState(TurretLoaderComponent loader)
    {
        foreach(var turret in loader.BoundTurret)
            BoundTurret.Add(turret);

        ContainerSlotID = loader.ContainerSlot?.ID;
        MaxContainerCapacity = loader.MaxContainerCapacity;
        ContainerID = loader.AmmoContainer?.ID;
    }
}
