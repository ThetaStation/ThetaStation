using System.Linq;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Robust.Shared.Serialization;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class TurretLoaderSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slotSys = default!;
    [Dependency] private readonly SharedAudioSystem _audioSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TurretLoaderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TurretLoaderComponent, ComponentRemove>(OnRemoval);

        SubscribeLocalEvent<TurretLoaderComponent, EntInsertedIntoContainerMessage>(OnContainerInsert);
        SubscribeLocalEvent<TurretLoaderComponent, EntRemovedFromContainerMessage>(OnContainerRemove);

        SubscribeLocalEvent<TurretLoaderComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<TurretLoaderComponent, ThrowHitByEvent>(HandleThrowCollide);
        SubscribeLocalEvent<TurretLoaderComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TurretLoaderComponent, TurretLoaderAfterShotMessage>(AfterShot);
        SubscribeLocalEvent<TurretLoaderComponent, ComponentGetState>(GetLoaderState);
        SubscribeLocalEvent<TurretLoaderComponent, NewLinkEvent>(OnLink);
    }

    private void GetLoaderState(EntityUid uid, TurretLoaderComponent loader, ref ComponentGetState args)
    {
        UpdateAmmoContainer(loader);
        args.State = new TurretLoaderState(
            GetNetEntity(loader.BoundTurret),
            loader.MaxContainerCapacity,
            loader.ContainerSlot?.ID,
            loader.AmmoContainer?.ID
            );
    }

    public void SetupLoader(EntityUid uid, TurretLoaderComponent loader, EntityUid? turretUid = null)
    {
        if (!EntityManager.EntityExists(loader.BoundTurret) && turretUid != null)
        {
            loader.BoundTurret = turretUid.Value;
        }
        else
        {
            if (CheckNetwork(uid, out EntityUid turret))
                loader.BoundTurret = turret;
        }

        if (EntityManager.TryGetComponent<ItemSlotsComponent>(uid, out var slots))
        {
            loader.ContainerSlot = slots.Slots["ammoContainer"];

            if (loader.BoundTurret != null)
            {
                if (EntityManager.TryGetComponent<CannonComponent>(loader.BoundTurret, out var cannon))
                {
                    cannon.BoundLoader = loader;
                    cannon.BoundLoaderEntity = uid;
                    Dirty(cannon);
                }
            }
        }

        Dirty(loader);
    }

    private void UpdateAmmoContainer(TurretLoaderComponent loader)
    {
        ContainerAmmoProviderComponent? turretContainer = null;
        List<string>? turretAmmoProts = null;

        if (loader.BoundTurret != null && EntityManager.EntityExists(loader.BoundTurret))
        {
            turretContainer = EntityManager.EnsureComponent<ContainerAmmoProviderComponent>(loader.BoundTurret.Value);
            turretContainer.ProviderUid = null;
            turretContainer.Container = "";
            turretAmmoProts = EntityManager.GetComponent<CannonComponent>(loader.BoundTurret.Value).AmmoPrototypes;
        }

        loader.AmmoContainer = null;
        loader.MaxContainerCapacity = 0;

        var container = loader.ContainerSlot?.Item;
        if (EntityManager.TryGetComponent<StorageComponent>(container, out var storage))
        {
            if (turretAmmoProts != null && storage.Container?.ContainedEntities.Count > 0 &&
                !turretAmmoProts.Contains(EntityManager
                                              .GetComponent<MetaDataComponent>(storage.Container.ContainedEntities[0])
                                              .EntityPrototype?.ID ?? ""))
            {
                _audioSys.PlayPredicted(loader.InvalidAmmoTypeSound, loader.Owner, loader.Owner);

                if (loader.ContainerSlot == null)
                    return;
                _slotSys.TryEject(loader.Owner, loader.ContainerSlot, loader.Owner, out _);
            }

            loader.AmmoContainer = storage.Container;
            loader.MaxContainerCapacity = storage.MaxSlots ?? storage.MaxTotalWeight;

            if (turretContainer != null && storage.Container != null)
            {
                turretContainer.ProviderUid = container;
                turretContainer.Container = storage.Container.ID;
            }
        }

        Dirty(loader);
        if (turretContainer != null)
            Dirty(turretContainer);
    }

    private bool CheckNetwork(EntityUid uid, out EntityUid turret)
    {
        if (EntityManager.TryGetComponent<DeviceLinkSourceComponent>(uid, out var source))
        {
            if (source.LinkedPorts.Count > 0)
            {
                turret = source.LinkedPorts.Keys.First();
                return true;
            }
        }

        turret = EntityUid.Invalid;
        return false;
    }

    private void OnInit(EntityUid uid, TurretLoaderComponent loader, ComponentInit args)
    {
        SetupLoader(uid, loader);
        UpdateAmmoContainer(loader);
    }

    private void OnRemoval(EntityUid uid, TurretLoaderComponent loader, ComponentRemove args)
    {
        if (EntityManager.EntityExists(loader.BoundTurret))
        {
            if (EntityManager.TryGetComponent<CannonComponent>(loader.BoundTurret, out var cannon))
                cannon.BoundLoader = null;
            if (EntityManager.TryGetComponent<ContainerAmmoProviderComponent>(loader.BoundTurret, out var container))
            {
                container.ProviderUid = null;
                container.Container = "";
            }
        }
    }

    private void OnContainerInsert(EntityUid uid, TurretLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        UpdateAmmoContainer(loader);
    }

    private void OnContainerRemove(EntityUid uid, TurretLoaderComponent loader, EntRemovedFromContainerMessage args)
    {
        UpdateAmmoContainer(loader);
    }

    private void OnLink(EntityUid uid, TurretLoaderComponent loader, NewLinkEvent args)
    {
        SetupLoader(uid, loader, args.Sink);
    }

    private void AfterShot(EntityUid uid, TurretLoaderComponent loader, TurretLoaderAfterShotMessage args)
    {
        if (loader.AmmoContainer != null && loader.ContainerSlot != null)
        {
            if (loader.AmmoContainer.ContainedEntities.Count == 0)
                _slotSys.TryEject(uid, loader.ContainerSlot, uid, out _);
        }
    }

    /// <summary>
    /// Throwing ammo in loads it up.
    /// </summary>
    private void HandleThrowCollide(EntityUid uid, TurretLoaderComponent component, ThrowHitByEvent args)
    {
        if (component.ContainerSlot == null)
            return;

        if (component.ContainerSlot.HasItem && !_slotSys.TryEject(uid, component.ContainerSlot, null, out var item))
            return;

        _slotSys.TryInsert(uid, component.ContainerSlot, args.Thrown, args.Component.Thrower);
    }

    private void OnHandInteract(EntityUid uid, TurretLoaderComponent component, InteractHandEvent args)
    {
        if (component.ContainerSlot == null)
            return;
        _slotSys.TryEject(uid, component.ContainerSlot, null, out var item);
    }

    private void OnExamined(EntityUid uid, TurretLoaderComponent component, ExaminedEvent args)
    {
        var ammoCount = 0;

        //slot contents check may seem redundant, when AmmoContainer is updated based on slot contents anyway when loader component
        //changes state, however due to unknown reasons AmmoContainer is sometimes updated incorrectly, so it's better to double-check
        if (component.AmmoContainer != null && component.ContainerSlot?.Item != null)
            ammoCount = component.AmmoContainer.ContainedEntities.Count;

        args.PushMarkup(Loc.GetString("shipevent-turretloader-ammocount-examine", ("count", ammoCount)));
    }
}

public sealed class TurretLoaderAfterShotMessage : EntityEventArgs { }
