using System.Linq;
using Content.Server.Storage.Components;
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
using Robust.Shared.Serialization;
using FastAccessors;

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
        args.State = new TurretLoaderState(loader);
    }

    public void BindTurret(EntityUid uid, TurretLoaderComponent loader)
    {
        CheckNetwork(uid, out List<EntityUid>? turretList);

        if (turretList != null)
            loader.BoundTurret = turretList.ToList();

        LinkLoaderToTurret(uid, loader);
    }

    public void SetupLoader(EntityUid uid, TurretLoaderComponent loader, EntityUid turretUid)
    {
        loader.BoundTurret.Add(turretUid);

        LinkLoaderToTurret(uid, loader);
    }

    public void LinkLoaderToTurret(EntityUid uid, TurretLoaderComponent loader)
    {
        if (EntityManager.TryGetComponent<ItemSlotsComponent>(uid, out var slots))
        {
            loader.ContainerSlot = slots.Slots["ammoContainer"];

            if (loader.BoundTurret != null)
            {
                foreach (var turret in loader.BoundTurret)
                {
                    if (EntityManager.TryGetComponent<CannonComponent>(turret, out var cannon))
                    {
                        cannon.BoundLoader = loader;
                        cannon.BoundLoaderEntity = uid;
                        Dirty(cannon);
                    }
                }
            }
        }

        Dirty(loader);
    }

    private void UpdateAmmoContainer(TurretLoaderComponent loader)
    {
        ContainerAmmoProviderComponent? turretContainer = null;
        List<string>? turretAmmoProts = null;

        foreach (var turret in loader.BoundTurret)
        {
            if (EntityManager.EntityExists(turret))
            {
                turretContainer = EntityManager.EnsureComponent<ContainerAmmoProviderComponent>(turret);
                turretContainer.ProviderUid = null;
                turretContainer.Container = "";
                turretAmmoProts = EntityManager.GetComponent<CannonComponent>(turret).AmmoPrototypes;
            }
        }

        loader.AmmoContainer = null;
        loader.MaxContainerCapacity = 0;

        var container = loader.ContainerSlot?.Item;
        if (EntityManager.TryGetComponent<ServerStorageComponent>(container, out var storage))
        {
            if (storage.Storage != null)
            {
                if (turretAmmoProts != null && storage.Storage.ContainedEntities.Count > 0 &&
                    !turretAmmoProts.Contains(EntityManager
                                                  .GetComponent<MetaDataComponent>(storage.Storage.ContainedEntities[0])
                                                  .EntityPrototype?.ID ?? ""))
                {
                    _audioSys.PlayPredicted(loader.InvalidAmmoTypeSound, loader.Owner, loader.Owner);

                    if (loader.ContainerSlot == null)
                        return;
                    _slotSys.TryEject(loader.Owner, loader.ContainerSlot, loader.Owner, out _);
                }

                loader.AmmoContainer = storage.Storage;
                loader.MaxContainerCapacity = storage.StorageCapacityMax;

                if (turretContainer != null)
                {
                    turretContainer.ProviderUid = container;
                    turretContainer.Container = storage.Storage.ID;
                }
            }
        }

        Dirty(loader);
        if (turretContainer != null)
            Dirty(turretContainer);
    }

    private bool CheckNetwork(EntityUid uid, out List<EntityUid>? turretList)
    {
        if (EntityManager.TryGetComponent<DeviceLinkSourceComponent>(uid, out var source))
        {
            if (source.LinkedPorts.Count > 0)
            {
                turretList = source.LinkedPorts.Keys.ToList();
                return true;
            }
        }

        turretList = null;
        return false;
    }

    private void OnInit(EntityUid uid, TurretLoaderComponent loader, ComponentInit args)
    {
        BindTurret(uid, loader);
        UpdateAmmoContainer(loader);
    }

    private void OnRemoval(EntityUid uid, TurretLoaderComponent loader, ComponentRemove args)
    {
        foreach (var turret in loader.BoundTurret)
        {
            if (EntityManager.EntityExists(turret))
            {
                if (EntityManager.TryGetComponent<CannonComponent>(turret, out var cannon))
                    cannon.BoundLoader = null;
                if (EntityManager.TryGetComponent<ContainerAmmoProviderComponent>(turret, out var container))
                {
                    container.ProviderUid = null;
                    container.Container = "";
                }
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
