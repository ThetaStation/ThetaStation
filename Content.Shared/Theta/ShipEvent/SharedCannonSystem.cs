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

    private const int CollisionCheckDistance = 20;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestCannonShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopCannonShootEvent>(OnStopShootRequest);
        SubscribeLocalEvent<CannonComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<CannonComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, CannonComponent cannon, ComponentInit args)
    {
        cannon.FreeFiringRanges = CalculateFiringRanges(uid, GetCannonGun(uid)!);
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

    private bool CanShoot(RequestCannonShootEvent args, GunComponent gun, CannonComponent cannon)
    {
        if (!_gunSystem.CanShoot(gun))
            return false;
        
        TransformComponent cannonTransform = Transform(args.CannonUid);
        TransformComponent pilotTransform = Transform(args.PilotUid);
        if (!pilotTransform.GridUid.Equals(cannonTransform.GridUid))
            return false;

        Angle firingAngle = Angle.FromWorldVec(args.Coordinates - _transform.GetWorldPosition(cannonTransform)).Reduced();
        foreach ((Angle a, Angle b) in cannon.FreeFiringRanges)
        {
            if (a.Theta > firingAngle && firingAngle > b)
                return true;
        }
        
        return false;
    }

    protected virtual void OnAnchorChanged(EntityUid uid, CannonComponent cannon, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            StopShoot(uid);
            return;
        }

        cannon.FreeFiringRanges = CalculateFiringRanges(uid, GetCannonGun(uid)!);
    }
    
    private List<(Angle, Angle)> CalculateFiringRanges(EntityUid uid, GunComponent gun)
    {
        List<(Angle, Angle)> ranges = new();
        TransformComponent gridForm = EntityManager.GetComponent<TransformComponent>(Transform(uid).ParentUid);

        //todo: I haven't figured out how to get all fixtures in range. definitely should be possible, but...
        foreach (EntityUid childUid in gridForm.ChildEntities)
        {
            TransformComponent form = Transform(childUid);
            Vector2 v = _transform.GetWorldPosition(form)+0.5f - _transform.GetWorldPosition(uid)+0.5f;
            float d = v.Length;
            if (d > CollisionCheckDistance)
                continue;

            Angle w, c, s0, e0;
            
            c = Angle.FromWorldVec(v);
            //0.5/0.25 is triangle's base divided by two, squared. so in case if it's straight angle it's 0.5^2
            //or (sqrt(2)/2)^2 for diagonal
            w = 2*Math.Asin(d / Math.Sqrt(c % Math.PI*0.5 == 0 ? 0.25 : 0.5 + d*d));
            
            //0.08 is 5 degrees offset, just to be safe
            s0 = (c - w + gun.MaxAngle + 0.08).Reduced();
            e0 = (c + w - gun.MaxAngle - 0.08).Reduced();

            bool ov = false;
            List<(Angle, Angle)> uranges = new();
            foreach ((Angle s1, Angle e1) in ranges)
            {
                if (s0 > s1 && s0 < e1 || e0 > s1 && e0 < e1)
                {
                    uranges.Add((Math.Min(s0, s1), Math.Max(e0, e1)));
                    ov = true;
                }
                else
                {
                    uranges.Add((s1, e1));
                }
            }
            
            if(!ov)
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
