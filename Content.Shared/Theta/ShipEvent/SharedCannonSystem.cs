using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent;

public abstract class SharedCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ItemSlotsSystem _slotSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestCannonShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopCannonShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<CannonComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnShootRequest(RequestCannonShootEvent ev, EntitySessionEventArgs args)
    {
        var gun = GetCannonGun(ev.CannonUid);
        var cannon = EntityManager.GetComponent<CannonComponent>(ev.CannonUid);
        if (gun == null || !CanShoot(ev, gun, cannon))
        {
            StopShoot(ev.CannonUid);
            return;
        }

        var mapCoords = new MapCoordinates(ev.Coordinates, Transform(ev.CannonUid).MapID);
        var coords = EntityCoordinates.FromMap(ev.CannonUid, mapCoords, _transform);
        _gunSystem.AttemptShoot(ev.PilotUid, ev.CannonUid, gun, coords);
    }

    public Angle Max(params Angle[] args)
    {
        Angle max = args[0];
        foreach (Angle d in args)
        {
            if (d > max)
                max = d;
        }
        return max;
    }
    
    public Angle Min(params Angle[] args)
    {
        Angle min = args[0];
        foreach (Angle d in args)
        {
            if (d < min)
                min = d;
        }
        return min;
    }
    
    public Angle ReducedAndPositive(Angle a)
    {
        a = a.Reduced();
        if (a < 0)
            a += 2 * Math.PI;
        return a;
    }
    
    public bool IsInsideSector(Angle x, Angle s, Angle w)
    {
        Angle d = Angle.ShortestDistance(s, x);
        return Math.Sign(w) == Math.Sign(d) ? Math.Abs(w) >= Math.Abs(d) : Math.Abs(w) >= Math.Tau - Math.Abs(d);
    }

    public bool AreSectorsOverlapping(Angle s0, Angle w0, Angle s1, Angle w1)
    {
        Angle e0 = ReducedAndPositive(s0 + w0);
        Angle e1 = ReducedAndPositive(s1 + w1);
        return IsInsideSector(s0, s1, w1) || IsInsideSector(s1, s0, w0) ||
               IsInsideSector(e0, s1, w1) || IsInsideSector(e1, s0, w0);
    }

    public (Angle, Angle) CombinedSector(Angle s0, Angle w0, Angle s1, Angle w1)
    {
        (Angle e0, Angle e1) = (ReducedAndPositive(s0 + w0), ReducedAndPositive(s1 + w1));
        Angle[] angles = {s0, e0, s1, e1};
        
        //edge case - full overlap
        if (IsInsideSector(s0, s1, w1) && IsInsideSector(e0, s1, w1))
            return (s1, w1);
        if (IsInsideSector(s1, s0, w0) && IsInsideSector(e1, s0, w0))
            return (s0, w0);

        Angle s, w, m, d;
        s = Max(angles);
        w = s == s0 ? w0 : s == e0 ? -w0 : s == s1 ? w1 : s == e1 ? -w1 : 0;

        m = 0;
        foreach (Angle a in angles)
        {
            if (Math.Abs(s - a) < 0.1)
                continue;
            
            d = Angle.ShortestDistance(s, a);
            d = Math.Sign(d) == Math.Sign(w) ? d : Math.Tau - Math.Abs(d);
            if (Double.Abs(d) > Double.Abs(m))
                m = d;
        }

        return (s, m);
    }

    private bool CanShoot(RequestCannonShootEvent args, GunComponent gun, CannonComponent cannon)
    {
        if (!_gunSystem.CanShoot(gun))
            return false;
        
        TransformComponent cannonTransform = Transform(args.CannonUid);
        TransformComponent pilotTransform = Transform(args.PilotUid);
        if (!pilotTransform.GridUid.Equals(cannonTransform.GridUid))
            return false;

        Angle firingAngle = ReducedAndPositive(new Angle(args.Coordinates - _transform.GetWorldPosition(cannonTransform)) - 
                                               _transform.GetWorldRotation(Transform(cannonTransform.GridUid ?? args.CannonUid)));
        foreach ((Angle s, Angle w) in cannon.ObstructedRanges)
        {
            if (IsInsideSector(firingAngle, s, w))
                return false;
        }
        
        return true;
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
        StopShoot(ev.CannonUid);
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
    public EntityUid CannonUid;
    public EntityUid PilotUid;
    public Vector2 Coordinates;
}

/// <summary>
/// Raised on the client to request it would like to stop shooting.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestStopCannonShootEvent : EntityEventArgs
{
    public EntityUid CannonUid;
}
