using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using System.Numerics;
using Content.Shared.Interaction.Events;

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
        SubscribeLocalEvent<CannonComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CannonComponent, ChangeDirectionAttemptEvent>(OnAttemptRotate);
    }

    private void OnAttemptRotate(EntityUid uid, CannonComponent component, ChangeDirectionAttemptEvent args)
    {
        if(!component.Rotatable)
            args.Cancel();
    }

    private void OnInit(EntityUid uid, CannonComponent cannon, ComponentInit args)
    {
        foreach (IComponent comp in EntityManager.GetComponents(uid))
        {
            if (comp is AmmoProviderComponent provider)
            {
                cannon.AmmoProvider = provider;
                break;
            }
        }
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

        var cannonTransform = Transform(ev.CannonUid);
        if (cannonTransform.GridUid == null)
        {
            StopShoot(ev.CannonUid);
            return;
        }

        var angleVector = cannonTransform.LocalRotation.ToWorldVec();
        var angleCoords = new EntityCoordinates(cannonTransform.GridUid.Value, angleVector);
        _gunSystem.AttemptShoot(ev.PilotUid, ev.CannonUid, gun, angleCoords + cannonTransform.Coordinates);
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

    public Angle ReducedAndPositive(Angle x)
    {
        x = x.Reduced();
        if (x < 0)
            x += 2 * Math.PI;
        return x;
    }

    public bool IsInsideSector(Angle x, Angle start, Angle width)
    {
        Angle dist = Angle.ShortestDistance(start, x);
        return Math.Sign(width) == Math.Sign(dist) ? Math.Abs(width) >= Math.Abs(dist) : Math.Abs(width) >= Math.Tau - Math.Abs(dist);
    }

    public bool AreSectorsOverlapping(Angle start0, Angle width0, Angle start1, Angle width1)
    {
        Angle end0 = ReducedAndPositive(start0 + width0);
        Angle end1 = ReducedAndPositive(start1 + width1);
        return IsInsideSector(start0, start1, width1) || IsInsideSector(start1, start0, width0) ||
               IsInsideSector(end0, start1, width1) || IsInsideSector(end1, start0, width0);
    }

    //only accepts sectors with positive width
    public (Angle, Angle) CombinedSector(Angle start0, Angle width0, Angle start1, Angle width1)
    {
        Angle startlow, widthlow, starthigh, widthhigh, disthigh;
        (startlow, widthlow, starthigh, widthhigh) = IsInsideSector(start1, start0, width0) ? (start0, width0, start1, width1) : (start1, width1, start0, width0);
        disthigh = ReducedAndPositive(starthigh + widthhigh) > startlow ? starthigh + widthhigh - startlow : Math.Tau - startlow + ReducedAndPositive(starthigh + widthhigh);

        return (startlow, Math.Min(Math.Max(widthlow, disthigh), Math.Tau));
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
        foreach ((Angle start, Angle w) in cannon.ObstructedRanges)
        {
            if (IsInsideSector(firingAngle, start, w))
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
