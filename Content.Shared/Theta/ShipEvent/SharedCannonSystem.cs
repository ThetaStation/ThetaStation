using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using System.Numerics;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Network;

namespace Content.Shared.Theta.ShipEvent;

public abstract class SharedCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ItemSlotsSystem _slotSys = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestCannonShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopCannonShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<CannonComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<CannonComponent, ChangeDirectionAttemptEvent>(OnAttemptRotate);
        SubscribeLocalEvent<CannonComponent, TakeAmmoEvent>(OnAmmoRequest);
        SubscribeLocalEvent<CannonComponent, GetAmmoCountEvent>(OnAmmoCount);
    }

    private void OnAmmoRequest(EntityUid uid, CannonComponent cannon, TakeAmmoEvent ev)
    {
        if (!TryComp<TurretLoaderComponent>(cannon.BoundLoaderUid, out var loader))
            return;

        if (TryComp<TurretAmmoContainerComponent>(loader.ContainerSlot?.Item, out var ammoContainer))
        {
            for (int i = 0; i < ev.Shots && i < ammoContainer.AmmoCount; i++)
            {
                EntityUid roundUid = Spawn(ammoContainer.AmmoPrototype);
                ev.Ammo.Add((roundUid, _gunSystem.EnsureShootable(roundUid)));
            }

            if (!_netMan.IsClient)
                ammoContainer.AmmoCount -= ev.Shots;

            if (ammoContainer.AmmoCount < 0)
                ammoContainer.AmmoCount = 0;
        }
    }

    private void OnAmmoCount(EntityUid uid, CannonComponent cannon, ref GetAmmoCountEvent ev)
    {
        if (!TryComp<TurretLoaderComponent>(cannon.BoundLoaderUid, out var loader))
            return;

        if (TryComp<TurretAmmoContainerComponent>(loader.ContainerSlot?.Item, out var ammoContainer))
        {
            ev.Capacity = ammoContainer.MaxAmmoCount;
            ev.Count = ammoContainer.AmmoCount;
        }
    }

    private void OnAttemptRotate(EntityUid uid, CannonComponent component, ChangeDirectionAttemptEvent args)
    {
        if (!component.Rotatable)
            args.Cancel();
    }

    private void OnShootRequest(RequestCannonShootEvent ev, EntitySessionEventArgs args)
    {
        var evCannonUid = GetEntity(ev.CannonUid);
        var gun = GetCannonGun(evCannonUid);
        var cannon = EntityManager.GetComponent<CannonComponent>(evCannonUid);
        if (gun == null || !CanShoot(ev, gun, cannon))
        {
            StopShoot(evCannonUid);
            return;
        }

        var cannonTransform = Transform(GetEntity(ev.CannonUid));
        if (cannonTransform.GridUid == null)
        {
            StopShoot(GetEntity(ev.CannonUid));
            return;
        }

        var mapCoords = new MapCoordinates(ev.Coordinates, Transform(evCannonUid).MapID);
        var coords = EntityCoordinates.FromMap(evCannonUid, mapCoords, _transform);
        _gunSystem.AttemptShoot(GetEntity(ev.PilotUid), evCannonUid, gun, coords);
    }

    private bool CanShoot(RequestCannonShootEvent args, GunComponent gun, CannonComponent cannon)
    {
        if (!_gunSystem.CanShoot(gun))
            return false;

        TransformComponent cannonTransform = Transform(GetEntity(args.CannonUid));
        TransformComponent pilotTransform = Transform(GetEntity(args.PilotUid));
        if (!pilotTransform.GridUid.Equals(cannonTransform.GridUid))
            return false;

        Angle firingAngle = ThetaHelpers.AngNormal(new Angle(args.Coordinates - _transform.GetWorldPosition(cannonTransform)) -
                                               _transform.GetWorldRotation(Transform(cannonTransform.GridUid ?? GetEntity(args.CannonUid))));
        foreach ((Angle start, Angle width) in cannon.ObstructedRanges)
        {
            if (ThetaHelpers.AngInSector(firingAngle, start, width))
                return false;
        }

        return true;
    }

    public (int ammo, int maxAmmo) GetCannonAmmoCount(EntityUid uid, CannonComponent? cannon)
    {
        if (!Resolve(uid, ref cannon))
            return (0, 0);

        var ammoCountEv = new GetAmmoCountEvent();
        RaiseLocalEvent(uid, ref ammoCountEv);

        return (ammoCountEv.Count, ammoCountEv.Capacity);
    }

    protected virtual void OnAnchorChanged(EntityUid uid, CannonComponent cannon, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
            StopShoot(uid);
    }

    public GunComponent? GetCannonGun(EntityUid uid)
    {
        return !TryComp<GunComponent>(uid, out var gun) ? null : gun;
    }

    private void OnStopShootRequest(RequestStopCannonShootEvent ev)
    {
        StopShoot(GetEntity(ev.CannonUid));
    }

    private void StopShoot(EntityUid cannonUid)
    {
        var gun = GetCannonGun(cannonUid);
        if (gun == null || gun.ShotCounter == 0)
            return;

        gun.ShotCounter = 0;
        gun.ShootCoordinates = null;
        Dirty(cannonUid, gun);
    }
}

[Serializable, NetSerializable]
public sealed class RotateCannonsEvent : EntityEventArgs
{
    public readonly Vector2 Coordinates;

    public readonly List<NetEntity> Cannons;

    public RotateCannonsEvent(Vector2 coordinates, List<NetEntity> cannons)
    {
        Coordinates = coordinates;
        Cannons = cannons;
    }
}

/// <summary>
/// Raised on the client to indicate it'd like to shoot.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestCannonShootEvent : EntityEventArgs
{
    public NetEntity CannonUid;
    public NetEntity PilotUid;
    public Vector2 Coordinates;
}

/// <summary>
/// Raised on the client to request it would like to stop shooting.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestStopCannonShootEvent : EntityEventArgs
{
    public NetEntity CannonUid;
}
