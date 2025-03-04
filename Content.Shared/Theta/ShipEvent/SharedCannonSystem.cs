using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System.Numerics;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Network;

namespace Content.Shared.Theta.ShipEvent;

//todo: like half of this is code isn't actually used on client and should be moved to server
public abstract class SharedCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
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
        ShootCannon(GetEntity(ev.CannonUid), GetEntity(ev.PilotUid), ev.Coordinates);
    }

    public void ShootCannon(EntityUid uid, EntityUid userUid, Vector2 coords)
    {
        var gun = CompOrNull<GunComponent>(uid);
        var cannon = EntityManager.GetComponent<CannonComponent>(uid);
        if (gun == null || !CanShoot(uid, gun, cannon, userUid, coords))
        {
            StopShoot(uid);
            return;
        }

        var form = Transform(uid);
        if (form.GridUid == null)
        {
            StopShoot(uid);
            return;
        }

        var mapCoords = new MapCoordinates(coords, form.MapID);
        var entCoords = _transform.ToCoordinates(uid, mapCoords);
        _gunSystem.AttemptShoot(uid, uid, gun, entCoords);
    }

    private bool CanShoot(EntityUid uid, GunComponent gun, CannonComponent cannon, EntityUid userUid, Vector2 coords)
    {
        if (!_gunSystem.CanShoot(gun))
            return false;

        TransformComponent cannonTransform = Transform(uid);
        TransformComponent pilotTransform = Transform(userUid);
        if (TryComp<ShipEventTeamMarkerComponent>(pilotTransform.GridUid, out var marker) &&
            marker.Team != null &&
            cannonTransform.GridUid != null &&
            !marker.Team.ShipGrids.Contains(cannonTransform.GridUid.Value))
            return false;

        Angle targetAngle = new Angle(coords - _transform.GetWorldPosition(cannonTransform));
        Angle firingAngle = ThetaHelpers.AngNormal(targetAngle - _transform.GetWorldRotation(cannonTransform.GridUid ?? uid));
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

    private void OnStopShootRequest(RequestStopCannonShootEvent ev)
    {
        StopShoot(GetEntity(ev.CannonUid));
    }

    private void StopShoot(EntityUid uid)
    {
        var gun = CompOrNull<GunComponent>(uid);
        if (gun == null || gun.ShotCounter == 0)
            return;

        _gunSystem.StopShooting(uid, gun);
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
