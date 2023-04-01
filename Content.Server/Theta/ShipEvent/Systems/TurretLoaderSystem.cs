using Content.Server.Storage.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Containers;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class TurretLoaderSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TurretLoaderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TurretLoaderComponent, ComponentRemove>(OnRemoval);
        SubscribeLocalEvent<TurretLoaderComponent, ContainerModifiedMessage>(OnContainerModification);
    }

    public List<EntityUid> GetLoaderAmmo(TurretLoaderComponent loader)
    {
        if (loader.AmmoContainer != null)
            return new List<EntityUid>(loader.AmmoContainer.ContainedEntities);

        return new List<EntityUid>();
    }

    public void SetupLoader(EntityUid uid, TurretLoaderComponent loader)
    {
        if (EntityManager.TryGetComponent<ItemSlotsComponent>(uid, out var slots))
        {
            loader.ContainerSlot = slots.Slots["ammoContainer"];

            if (loader.BoundTurret != EntityUid.Invalid)
            {
                if (EntityManager.TryGetComponent<CannonComponent>(loader.BoundTurret, out var cannon))
                    cannon.BoundLoader = loader;
            }
        }
    }
    
    private void OnInit(EntityUid uid, TurretLoaderComponent loader, ComponentInit args)
    {
        SetupLoader(uid, loader);
    }

    private void OnRemoval(EntityUid uid, TurretLoaderComponent loader, ComponentRemove args)
    {
        if (loader.BoundTurret != EntityUid.Invalid)
        {
            if (EntityManager.TryGetComponent<CannonComponent>(loader.BoundTurret, out var cannon))
                cannon.BoundLoader = null;
        }
    }
    
    private void OnContainerModification(EntityUid uid, TurretLoaderComponent loader, ContainerModifiedMessage args)
    {
        var container = loader.ContainerSlot?.Item;

        if (EntityManager.TryGetComponent<ServerStorageComponent>(container, out var storage))
        {
            if (storage.Storage != null)
                loader.AmmoContainer = storage.Storage;
        }

        loader.AmmoContainer = null;
        Dirty(loader);
    }
}
