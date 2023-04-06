using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent;

public abstract class SharedCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestCannonShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopCannonShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<CannonComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<CannonComponent, TakeAmmoEvent>(OnAmmoRequest);
        SubscribeLocalEvent<CannonComponent, GetAmmoCountEvent>(OnAmmoCount);
    }

    private void OnAmmoRequest(EntityUid uid, CannonComponent cannon, TakeAmmoEvent args)
    {
        var loader = cannon.BoundLoader;
        
        if (loader == null)
            return;

        if (loader.AmmoContainer != null)
        {
            for (int i = 0; i < args.Shots; i++)
            {
                if (!loader.AmmoContainer.ContainedEntities.Any())
                    break;

                var ent = loader.AmmoContainer.ContainedEntities[0];

                var prot = EntityManager.GetComponent<MetaDataComponent>(ent).EntityPrototype?.ID;

                if (prot == null)
                    continue;

                if (!cannon.AmmoPrototypes.Contains(prot))
                    continue;

                loader.AmmoContainer.Remove(ent);

                args.Ammo.Add((ent, EnsureComp<AmmoComponent>(ent)));
            }
        }
        
        Dirty(loader);
    }
    
    private void OnAmmoCount(EntityUid uid, CannonComponent cannon, ref GetAmmoCountEvent args)
    {
        var loader = cannon.BoundLoader;
        
        if (loader == null)
            return;
        
        if (loader.AmmoContainer == null)
        {
            args.Capacity = 0;
            args.Count = 0;
            return;
        }

        //there is no sense in setting this, since ammo may have different size & types and it's impossible to count how much shots we can fit
        args.Capacity = 0;
        
        args.Count = loader.AmmoContainer.ContainedEntities.Count;
    }

    private void OnShootRequest(RequestCannonShootEvent ev, EntitySessionEventArgs args)
    {
        var gun = GetCannonGun(ev.Cannon);
        if (gun == null || !CanShoot(ev, gun))
        {
            StopShoot(ev.Cannon);
            return;
        }

        var mapCoords = new MapCoordinates(ev.Coordinates, Transform(ev.Cannon).MapID);
        var coords = EntityCoordinates.FromMap(ev.Cannon, mapCoords, _transform);
        _gunSystem.AttemptShoot(ev.Pilot, ev.Cannon, gun, coords);
    }

    public bool CanShoot(RequestCannonShootEvent args, GunComponent gun)
    {
        if (!_gunSystem.CanShoot(gun))
            return false;

        var cannonTransform = Transform(args.Cannon);
        var pilotTransform = Transform(args.Pilot);
        if (!pilotTransform.GridUid.Equals(cannonTransform.GridUid))
            return false;

        var dir = args.Coordinates - _transform.GetWorldPosition(cannonTransform);
        var ray = new CollisionRay(_transform.GetWorldPosition(cannonTransform), dir.Normalized,
            (int) (CollisionGroup.Impassable));

        const int averageShipLength = 25;

        var rayCastResult = _physics.IntersectRay(cannonTransform.MapID, ray, averageShipLength).ToList();

        foreach (var result in rayCastResult)
        {
            if (Transform(result.HitEntity).GridUid == cannonTransform.GridUid)
                return false;
        }

        return true;
    }

    protected virtual void OnAnchorChanged(EntityUid uid, CannonComponent component, ref AnchorStateChangedEvent args)
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
        StopShoot(ev.Cannon);
    }

    private void StopShoot(EntityUid cannonUid)
    {
        var gun = GetCannonGun(cannonUid);
        if (gun == null || gun.ShotCounter == 0)
            return;

        gun.ShotCounter = 0;
        gun.ShootCoordinates = null;
        Dirty(gun);
    }
}

[Serializable, NetSerializable]
public sealed class RotateCannonsEvent : EntityEventArgs
{
    public readonly Vector2 Coordinates;

    public readonly List<EntityUid> Cannons;

    public RotateCannonsEvent(Vector2 coordinates, List<EntityUid> cannons)
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
    public EntityUid Cannon;
    public EntityUid Pilot;
    public Vector2 Coordinates;
}

/// <summary>
/// Raised on the client to request it would like to stop shooting.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestStopCannonShootEvent : EntityEventArgs
{
    public EntityUid Cannon;
}
