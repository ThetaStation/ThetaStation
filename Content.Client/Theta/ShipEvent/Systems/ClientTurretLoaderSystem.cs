using Content.Shared.Containers.ItemSlots;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
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
        SubscribeLocalEvent<TurretLoaderComponent, ComponentInit>(OnLoaderInit);
    }

    private void OnLoaderInit(EntityUid uid, TurretLoaderComponent loader, ComponentInit args)
    {
        TurretLoaderSyncMessage ev = new();
        ev.LoaderUid = GetNetEntity(uid);
        RaiseNetworkEvent(ev);
    }

    private void SetLoaderState(EntityUid uid, TurretLoaderComponent loader, ref ComponentHandleState args)
    {
        if (args.Current is not TurretLoaderState loaderState)
            return;

        loader.BoundTurretUid = GetEntity(loaderState.BoundTurret);

        if (loaderState.ContainerSlotID != null)
        {
            if (_slotSys.TryGetSlot(uid, loaderState.ContainerSlotID, out var slot))
                loader.ContainerSlot = slot;
        }

        if (!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
            return;

        bool loaded = TryComp<TurretAmmoContainerComponent>(loader.ContainerSlot?.Item, out _);
        sprite.LayerSetState(0, loaded ? "loader-loaded" : "loader");
    }
}
