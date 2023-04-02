using Content.Server.MachineLinking.Components;
using Content.Server.MachineLinking.System;
using Content.Server.Storage.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.MachineLinking.Events;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class TurretLoaderSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slotSys = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSys = default!;
    [Dependency] private readonly SignalLinkerSystem _sigSys = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TurretLoaderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TurretLoaderComponent, ComponentRemove>(OnRemoval);
        SubscribeLocalEvent<TurretLoaderComponent, EntInsertedIntoContainerMessage>(OnContainerInsert);
        SubscribeLocalEvent<TurretLoaderComponent, EntRemovedFromContainerMessage>(OnContainerRemove);
        SubscribeLocalEvent<TurretLoaderComponent, TurretLoaderEjectRequest>(OnEject);
        SubscribeLocalEvent<TurretLoaderComponent, ComponentGetState>(GetLoaderState);
        SubscribeLocalEvent<TurretLoaderComponent, NewLinkEvent>(OnLink);
    }

    private void GetLoaderState(EntityUid uid, TurretLoaderComponent loader, ref ComponentGetState args)
    {
        args.State = new TurretLoaderState(loader);
    }

    private EntityUid GetLinkedTurret(EntityUid uid, TurretLoaderComponent loader)
    {
        if (EntityManager.TryGetComponent<SignalTransmitterComponent>(uid, out var sig))
        {
            if (sig.Outputs.ContainsKey("TurretLoaderSender"))
            {
                if (sig.Outputs["TurretLoaderSender"].Count > 0)
                    return sig.Outputs["TurretLoaderSender"][0].Uid;
            }
        }
        
        return EntityUid.Invalid;
    }

    public void SetupLoader(EntityUid uid, TurretLoaderComponent loader)
    {
        if (!loader.BoundTurret.IsValid())
            loader.BoundTurret = GetLinkedTurret(uid, loader);

        if (EntityManager.TryGetComponent<ItemSlotsComponent>(uid, out var slots))
        {
            loader.ContainerSlot = slots.Slots["ammoContainer"];

            if (loader.BoundTurret != EntityUid.Invalid)
            {
                if (EntityManager.TryGetComponent<CannonComponent>(loader.BoundTurret, out var cannon))
                {
                    cannon.BoundLoader = loader;
                    Dirty(cannon);
                }
            }
        }
        
        Dirty(loader);
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
    
    private void OnContainerInsert(EntityUid uid, TurretLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        var container = loader.ContainerSlot?.Item;

        if (EntityManager.TryGetComponent<ServerStorageComponent>(container, out var storage))
        {
            if (storage.Storage != null)
            {
                loader.AmmoContainer = storage.Storage;
                loader.MaxContainerCapacity = storage.StorageCapacityMax;
            }
        }
        
        _appearanceSys.SetData(uid, TurretLoaderVisuals.Loaded, true);

        Dirty(loader);
    }

    private void OnContainerRemove(EntityUid uid, TurretLoaderComponent loader, EntRemovedFromContainerMessage args)
    {
        loader.AmmoContainer = null;
        loader.MaxContainerCapacity = 0;
        
        _appearanceSys.SetData(uid, TurretLoaderVisuals.Loaded, false);

        Dirty(loader);
    }
    
    private void OnEject(EntityUid uid, TurretLoaderComponent loader, TurretLoaderEjectRequest args)
    {
        if (loader.ContainerSlot?.Item == null)
            return;

        _slotSys.TryEject(uid, loader.ContainerSlot, uid, out var _);
    }

    private void OnLink(EntityUid uid, TurretLoaderComponent loader, NewLinkEvent args)
    {
        SetupLoader(uid, loader);
    }
}
