using System.Linq;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Examine;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Content.Shared.Throwing;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class TurretLoaderSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaSys = default!;
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
        SubscribeLocalEvent<TurretLoaderComponent, InteractUsingEvent>(OnItemInteract, before: [typeof(SharedContainerSystem)]);
        SubscribeLocalEvent<TurretLoaderComponent, ThrowHitByEvent>(HandleThrowCollide);
        SubscribeLocalEvent<TurretLoaderComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<TurretLoaderComponent, TurretLoaderAfterShotMessage>(AfterShot);
        SubscribeLocalEvent<TurretLoaderComponent, ComponentGetState>(GetLoaderState);
        SubscribeLocalEvent<TurretLoaderComponent, NewLinkEvent>(OnLink);
        SubscribeLocalEvent<TurretAmmoContainerComponent, ExaminedEvent>(OnContainerExamine);
        SubscribeNetworkEvent<TurretLoaderSyncMessage>(OnSync);
    }

    private void OnContainerExamine(EntityUid uid, TurretAmmoContainerComponent container, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", "yellow"), ("count", container.AmmoCount)));
    }

    private void GetLoaderState(EntityUid uid, TurretLoaderComponent loader, ref ComponentGetState args)
    {
        args.State = new TurretLoaderState(GetNetEntity(loader.BoundTurretUid), loader.ContainerSlot?.ID);
    }

    public void SetupLoader(EntityUid uid, TurretLoaderComponent loader, EntityUid? turretUid = null)
    {
        if (EntityManager.EntityExists(loader.BoundTurretUid))
        {
            loader.BoundTurretUid = turretUid!.Value;
        }
        else
        {
            if (CheckNetwork(uid, out EntityUid turret))
                loader.BoundTurretUid = turret;
        }

        if (EntityManager.TryGetComponent<ItemSlotsComponent>(uid, out var slots))
        {
            loader.ContainerSlot = slots.Slots["ammoContainer"];

            if (loader.BoundTurretUid != null)
            {
                if (EntityManager.TryGetComponent<CannonComponent>(loader.BoundTurretUid, out var cannon))
                {
                    if (cannon.BoundLoaderUid == null)
                    {
                        cannon.BoundLoaderUid = uid;
                        Dirty(loader.BoundTurretUid.Value, cannon);
                    }
                    else
                    {
                        loader.BoundTurretUid = null;
                    }
                }
            }
        }

        Dirty(uid, loader);
    }

    //returns true if container is compatible with the loader
    private bool CheckContainer(
        EntityUid loaderUid,
        EntityUid containerUid,
        TurretLoaderComponent? loader = null,
        TurretAmmoContainerComponent? container = null)
    {
        if (!(Resolve(loaderUid, ref loader) && Resolve(containerUid, ref container)))
            return false;

        List<string>? turretAmmoProts = null;
        if (loader.BoundTurretUid != null && EntityManager.EntityExists(loader.BoundTurretUid))
            turretAmmoProts = EntityManager.GetComponent<CannonComponent>(loader.BoundTurretUid.Value).AmmoPrototypes;

        return container.AmmoCount != 0 && turretAmmoProts != null && turretAmmoProts.Contains(container.AmmoPrototype);
    }

    //ejects container if it's empty/incompatible
    private void CheckAndEjectContainer(EntityUid loaderUid, TurretLoaderComponent loader)
    {
        EntityUid? containerUid = loader.ContainerSlot?.Item;
        if (containerUid == null)
            return;

        if (!CheckContainer(loaderUid, containerUid.Value, loader))
        {
            if (loader.ContainerSlot == null)
                return;

            _slotSys.TryEject(loaderUid, loader.ContainerSlot, loaderUid, out _);
        }

        Dirty(loaderUid, loader);
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
        CheckAndEjectContainer(uid, loader);
    }

    private void OnRemoval(EntityUid uid, TurretLoaderComponent loader, ComponentRemove args)
    {
        if (Exists(loader.BoundTurretUid) && TryComp<CannonComponent>(loader.BoundTurretUid, out var cannon))
            cannon.BoundLoaderUid = null;
    }

    private void OnContainerInsert(EntityUid uid, TurretLoaderComponent loader, EntInsertedIntoContainerMessage args)
    {
        CheckAndEjectContainer(uid, loader);
    }

    private void OnContainerRemove(EntityUid uid, TurretLoaderComponent loader, EntRemovedFromContainerMessage args)
    {
        Dirty(uid, loader);
    }

    private void OnLink(EntityUid uid, TurretLoaderComponent loader, NewLinkEvent args)
    {
        SetupLoader(uid, loader, args.Sink);
    }

    private void AfterShot(EntityUid uid, TurretLoaderComponent loader, TurretLoaderAfterShotMessage args)
    {
        CheckAndEjectContainer(uid, loader);
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

    private void OnItemInteract(Entity<TurretLoaderComponent> uid, ref InteractUsingEvent args)
    {
        if (!CheckContainer(uid, args.Used, uid.Comp))
        {
            _audioSys.PlayPredicted(uid.Comp.InvalidAmmoSound, uid, uid);
            args.Handled = true;
        }
    }

    private void OnExamined(EntityUid uid, TurretLoaderComponent loader, ExaminedEvent args)
    {
        EntityUid? containerUid = loader.ContainerSlot?.Item;
        if (containerUid == null)
            return;

        if (TryComp<TurretAmmoContainerComponent>(containerUid, out var container))
            args.PushMarkup(Loc.GetString("shipevent-turretloader-ammocount-examine", ("count", container.AmmoCount)));
    }

    private void OnSync(TurretLoaderSyncMessage ev)
    {
        EntityUid uid = GetEntity(ev.LoaderUid);
        if (!uid.IsValid() || !TryComp<TurretLoaderComponent>(uid, out var loader))
            return;
        Dirty(uid, loader);
    }
}

public sealed class TurretLoaderAfterShotMessage : EntityEventArgs { }
