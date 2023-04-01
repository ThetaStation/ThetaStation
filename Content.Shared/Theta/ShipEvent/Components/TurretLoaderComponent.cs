using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Theta.ShipEvent.Components;

[RegisterComponent, NetworkedComponent]
public sealed class TurretLoaderComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)] public EntityUid BoundTurret = EntityUid.Invalid;
    
    /// <summary>
    /// Slot containing ammo container
    /// </summary>
    public ItemSlot? ContainerSlot;

    /// <summary>
    /// Entity container of inserted ammo container
    /// </summary>
    public Container? AmmoContainer;
}
