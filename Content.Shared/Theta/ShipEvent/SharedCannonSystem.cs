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

    public List<Vector2> dirs = new(); //debug, remove later

    private const int CollisionCheckDistance = 20;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestCannonShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopCannonShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<CannonComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<CannonComponent, ComponentInit>(OnInit);
    }

    protected virtual void OnInit(EntityUid uid, CannonComponent cannon, ComponentInit args)
    {
        cannon.ObstructedRanges = CalculateFiringRanges(uid, GetCannonGun(uid)!);
        Dirty(cannon);
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

    private Angle ReducedAndPositive(Angle a)
    {
        a = a.Reduced();
        if (a < 0)
            a += 2 * Math.PI;
        return a;
    }

    private bool CanShoot(RequestCannonShootEvent args, GunComponent gun, CannonComponent cannon)
    {
        if (!_gunSystem.CanShoot(gun))
            return false;
        
        TransformComponent cannonTransform = Transform(args.CannonUid);
        TransformComponent pilotTransform = Transform(args.PilotUid);
        if (!pilotTransform.GridUid.Equals(cannonTransform.GridUid))
            return false;

        Angle firingAngle = ReducedAndPositive(Angle.FromWorldVec(args.Coordinates - _transform.GetWorldPosition(cannonTransform)));
        foreach ((Angle s, Angle e) in cannon.ObstructedRanges)
        {
            if (firingAngle > s && firingAngle < e)
                return false;
        }
        
        return true;
    }

    protected virtual void OnAnchorChanged(EntityUid uid, CannonComponent cannon, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            StopShoot(uid);
            return;
        }

        cannon.ObstructedRanges = CalculateFiringRanges(uid, GetCannonGun(uid)!);
        Dirty(cannon);
    }
    
    private List<(Angle, Angle)> CalculateFiringRanges(EntityUid uid, GunComponent gun)
    {
        List<(Angle, Angle)> ranges = new();
        TransformComponent gridForm = EntityManager.GetComponent<TransformComponent>(Transform(uid).ParentUid);

        //todo: I haven't figured out how to get all fixtures in range. definitely should be possible, but...
        foreach (EntityUid childUid in gridForm.ChildEntities)
        {
            TransformComponent form = Transform(childUid);
            Vector2 dir = form.LocalPosition - Transform(uid).LocalPosition;
            dirs.Add(dir); //debug, remove later
            float dist = dir.Length;
            if (dist > CollisionCheckDistance || dist < 1)
                continue;

            Angle width, dirAngle, s0, e0;
            
            dirAngle = Angle.FromWorldVec(dir);
            //0.5/0.25 is triangle's base divided by two, squared. so in case if it's straight angle it's 0.5^2
            //or (sqrt(2)/2)^2 for diagonal.
            width = 2*Math.Asin(dist / Math.Sqrt(dirAngle % Math.PI*0.5 == 0 ? 0.25 : 0.5 + dist*dist));
            if (width == double.NaN)
                continue;

            //0.08 is 5 degrees offset, just to be safe
            s0 = ReducedAndPositive(dirAngle - width + gun.MaxAngle + 0.08);
            e0 = ReducedAndPositive(dirAngle + width - gun.MaxAngle - 0.08);

            bool overlap = false;
            List<(Angle, Angle)> uranges = new();
            foreach ((Angle s1, Angle e1) in ranges)
            {
                if (s0 >= s1 && s0 <= e1 || e0 >= s1 && e0 <= e1)
                {
                    uranges.Add((new Angle(Math.Min(s0, s1)), new Angle(Math.Max(e0, e1))));
                    overlap = true;
                }
                else
                {
                    uranges.Add((s1, e1));
                }
            }

            if (!overlap)
                uranges.Add((s0, e0));

            ranges = uranges;
        }
        
        return ranges;
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
