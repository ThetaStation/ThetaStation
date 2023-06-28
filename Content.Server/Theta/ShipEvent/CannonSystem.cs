using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Interaction;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.UI;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Physics.Components;

namespace Content.Server.Theta.ShipEvent;

public sealed class CannonSystem : SharedCannonSystem
{
    [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
    
    private const int CollisionCheckDistance = 10;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RotateCannonsEvent>(OnRotateCannons);
        SubscribeLocalEvent<CannonComponent, AmmoShotEvent>(AfterShot);
        SubscribeLocalEvent<CannonComponent, ComponentRemove>(OnRemoval);
    }

    public void RefreshFiringRanges(EntityUid uid, CannonComponent cannon)
    {
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
            float dist = dir.Length;
            if (dist > CollisionCheckDistance || dist < 1)
                continue;

            Angle dirAngle = ReducedAndPositive(new Angle(dir));
            bool culling = false;
            foreach ((Angle s, Angle w) in ranges)
            {
                //improves performance & helps to avoid some weird bugs with excess ranges
                if (IsInsideSector(dirAngle - 0.08, s, w) && IsInsideSector(dirAngle + 0.08, s, w))
                {
                    culling = true;
                    break;
                }
            }
            if (culling)
                continue;

            PhysicsComponent? phys = EntityManager.GetComponentOrNull<PhysicsComponent>(childUid);
            if (!form.Anchored || phys == null || !phys.Hard)
                continue;

            (Angle s0, Angle w0) = GetDirSector(dir, dirAngle);
            (Angle s2, Angle w2) = (s0, w0);
            
            List<(Angle, Angle)> overlaps = new();
            foreach ((Angle s1, Angle w1) in ranges)
            {
                if (AreSectorsOverlapping(s0, w0, s1, w1))
                {
                    (s2, w2) = CombinedSector(s2, w2, s1, w1);
                    overlaps.Add((s1, w1));
                }
            }
            
            foreach ((Angle s1, Angle w1) in overlaps)
            {
                ranges.Remove((s1, w1));
            }
            double ew = gun.MaxAngle + 0.04;
            (s2, w2) = w2 < 0 ? (s2 + ew, w2 - ew) : (s2 - ew, w2 + ew);
            s2 = ReducedAndPositive(s2);
            ranges.Add((s2, w2));
        }

        return ranges;
    }

    private (Angle, Angle) GetDirSector(Vector2 dir, Angle dirAngle)
    {
        Vector2 a, b;

        //this can be done without ugly conditional below, by rotating tile's square by dir angle and finding left- and rightmost points,
        //but this certainly will be heavier and less clear
        if (dirAngle % (Math.PI * 0.5) == 0)
        {
            switch (dirAngle.Theta)
            {
                case 0: case Math.Tau:
                    a = dir - 0.5f;
                    b = new Vector2(dir.X - 0.5f, dir.Y + 0.5f);
                    break;
                case Math.PI*0.5:
                    a = dir - 0.5f;
                    b = new Vector2(dir.X + 0.5f, dir.Y - 0.5f);
                    break;
                case Math.PI:
                    a = dir + 0.5f;
                    b = new Vector2(dir.X + 0.5f, dir.Y - 0.5f);
                    break;
                case Math.PI*1.5:
                    a = dir + 0.5f;
                    b = new Vector2(dir.X - 0.5f, dir.Y + 0.5f);
                    break;
                default:
                    return (double.NaN, double.NaN);
            }
        }
        else if (dirAngle > 0 && dirAngle < Math.PI * 0.5 || dirAngle > Math.PI && dirAngle < Math.PI * 1.5)
        {
            a = new Vector2(dir.X - 0.5f, dir.Y + 0.5f);
            b = new Vector2(dir.X + 0.5f, dir.Y - 0.5f);
        }
        else
        {
            a = dir + 0.5f;
            b = dir - 0.5f;
        }

        Angle aa = ReducedAndPositive(new Angle(a));
        Angle ba = ReducedAndPositive(new Angle(b));
        return (aa, Angle.ShortestDistance(aa, ba));
    }

    protected override void OnAnchorChanged(EntityUid uid, CannonComponent cannon, ref AnchorStateChangedEvent args)
    {
        //see comment on field itself
        if (cannon.FirstAnchor)
        {
            cannon.FirstAnchor = false;
            return;
        }
        
        base.OnAnchorChanged(uid, cannon, ref args);
        if (!args.Anchored)
            return;
        
        cannon.ObstructedRanges = CalculateFiringRanges(uid, GetCannonGun(uid)!);
        Dirty(cannon);
    }

    private void OnRemoval(EntityUid uid, CannonComponent cannon, ComponentRemove args)
    {
        if(cannon.BoundLoader != null)
            cannon.BoundLoader.BoundTurret = null;
    }

    private void OnRotateCannons(RotateCannonsEvent ev)
    {
        foreach (var uid in ev.Cannons)
        {
            _rotateToFaceSystem.TryFaceCoordinates(uid, ev.Coordinates);
        }
    }

    private void AfterShot(EntityUid entity, CannonComponent cannon, AmmoShotEvent args)
    {
        foreach(EntityUid projectile in args.FiredProjectiles)
        {
            var marker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(projectile);
            marker.Team = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(entity).Team;
        }

        if (cannon.BoundLoaderEntity != null)
            RaiseLocalEvent(cannon.BoundLoaderEntity.Value, new TurretLoaderAfterShotMessage());
    }
}
