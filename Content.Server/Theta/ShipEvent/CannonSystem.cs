using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Interaction;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Physics.Components;
using System.Numerics;

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
            float dist = dir.Length();
            if (dist > CollisionCheckDistance || dist < 1)
                continue;

            PhysicsComponent? phys = EntityManager.GetComponentOrNull<PhysicsComponent>(childUid);
            if (!form.Anchored || phys == null || !phys.Hard)
                continue;

            (Angle start0, Angle width0) = GetDirSector(dir);
            ranges.Add((start0, width0));

            (Angle start2, Angle width2) = (start0, width0);

            List<(Angle, Angle)> overlaps = new();
            foreach ((Angle start1, Angle width1) in ranges)
            {
                if (AreSectorsOverlapping(start0, width0, start1, width1))
                {
                    (start2, width2) = CombinedSector(start2, width2, start1, width1);
                    overlaps.Add((start1, width1));
                }
            }

            foreach ((Angle start1, Angle width1) in overlaps)
            {
                ranges.Remove((start1, width1));
            }
            ranges.Add((start2, width2));
        }

        double ew = gun.MaxAngle + 0.04;
        for (int i = 0; i < ranges.Count; i++)
        {
            ranges[i] = (ReducedAndPositive(ranges[i].Item1 - ew), ranges[i].Item2 + ew);
        }

        return ranges;
    }

    private (Angle, Angle) GetDirSector(Vector2 dir)
    {
        Angle dirAngle = ReducedAndPositive(new Angle(dir));
        Vector2 a, b;

        //this can be done without ugly conditional below, by rotating tile's square by dir angle and finding left- and rightmost points,
        //but this certainly will be heavier and less clear
        if (dirAngle % (Math.PI * 0.5) == 0)
        {
            switch (dirAngle.Theta)
            {
                case 0:
                case Math.Tau:
                    a = dir - Vector2Helpers.Half;
                    b = new Vector2(dir.X - 0.5f, dir.Y + 0.5f);
                    break;
                case Math.PI * 0.5:
                    a = dir - Vector2Helpers.Half;
                    b = new Vector2(dir.X + 0.5f, dir.Y - 0.5f);
                    break;
                case Math.PI:
                    a = dir + Vector2Helpers.Half;
                    b = new Vector2(dir.X + 0.5f, dir.Y - 0.5f);
                    break;
                case Math.PI * 1.5:
                    a = dir + Vector2Helpers.Half;
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
            a = dir + Vector2Helpers.Half;
            b = dir - Vector2Helpers.Half;
        }

        Angle aangle = ReducedAndPositive(new Angle(a));
        Angle bangle = ReducedAndPositive(new Angle(b));
        Angle w = Angle.ShortestDistance(aangle, bangle);
        if (w < 0)
        {
            aangle = bangle;
            w = -w;
        }

        return (aangle, w);
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
        if (cannon.BoundLoader != null)
            cannon.BoundLoader.BoundTurret = null;
    }

    private void OnRotateCannons(RotateCannonsEvent ev)
    {
        foreach (var uid in ev.Cannons)
        {
            var cannon = EntityManager.GetComponent<CannonComponent>(uid);
            if (!cannon.Rotatable)
                continue;

            _rotateToFaceSystem.TryFaceCoordinates(uid, ev.Coordinates);
        }
    }

    private void AfterShot(EntityUid entity, CannonComponent cannon, AmmoShotEvent args)
    {
        foreach (EntityUid projectile in args.FiredProjectiles)
        {
            var marker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(projectile);
            marker.Team = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(entity).Team;
        }

        if (cannon.BoundLoaderEntity != null)
            RaiseLocalEvent(cannon.BoundLoaderEntity.Value, new TurretLoaderAfterShotMessage());
    }
}
