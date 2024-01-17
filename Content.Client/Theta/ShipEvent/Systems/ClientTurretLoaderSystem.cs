using Content.Shared.Containers.ItemSlots;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class ClientTurretLoaderSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _contSys = default!;
    [Dependency] private readonly ItemSlotsSystem _slotSys = default!;

    //So, what does this dict do? Some components receive state update even before initialization, which breaks appearance & other stuff
    //In theory this should not happen, but since it does, we need this lookup
    private Dictionary<TurretLoaderComponent, ComponentHandleState> queuedStateUpdates = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TurretLoaderComponent, ComponentHandleState>(SetLoaderState);
        SubscribeLocalEvent<TurretLoaderComponent, ComponentInit>(OnLoaderInit);
    }

    private void OnLoaderInit(EntityUid uid, TurretLoaderComponent loader, ComponentInit args)
    {
        if (queuedStateUpdates.ContainsKey(loader))
        {
            ComponentHandleState state = queuedStateUpdates[loader];
            SetLoaderState(uid, loader, ref state);
            queuedStateUpdates.Remove(loader);
        }
    }

    private void SetLoaderState(EntityUid uid, TurretLoaderComponent loader, ref ComponentHandleState args)
    {
        if (!loader.Initialized)
        {
            queuedStateUpdates[loader] = args;
            return;
        }

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
