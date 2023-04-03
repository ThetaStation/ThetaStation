using Content.Shared.Containers.ItemSlots;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class ClientTurretLoaderSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _contSys = default!;
    [Dependency] private readonly ItemSlotsSystem _slotSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TurretLoaderComponent, ComponentHandleState>(SetLoaderState);
    }

    private void SetLoaderState(EntityUid uid, TurretLoaderComponent loader, ref ComponentHandleState args)
    {
        if (args.Current is not TurretLoaderState state) 
            return;
        var loaderState = (TurretLoaderState)args.Current;
        
        loader.BoundTurret = loaderState.BoundTurret;
        loader.MaxContainerCapacity = loaderState.MaxContainerCapacity;
        loader.CurrentContainerCapacity = loaderState.CurrentContainerCapacity;

        if (state.ContainerSlotID != null)
        {
            if (_slotSys.TryGetSlot(uid, state.ContainerSlotID, out var slot))
                loader.ContainerSlot = slot;
        }

        if (EntityManager.TryGetComponent<ContainerManagerComponent>(uid, out var contMan) &&
            state.ContainerID != null)
        {
            var ammoContainer = loader.ContainerSlot?.Item;

            if (ammoContainer == null)
                return;
            
            if (_contSys.TryGetContainer((EntityUid)ammoContainer, state.ContainerID, out var cont))
                loader.AmmoContainer = (Container)cont;
        }
    }
}
