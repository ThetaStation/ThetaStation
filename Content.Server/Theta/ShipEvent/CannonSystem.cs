using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Console;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.Theta;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
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
        SubscribeLocalEvent<CannonComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CannonComponent, ComponentRemove>(OnRemoval);
        SubscribeLocalEvent<CannonComponent, NewLinkEvent>(OnLink);
        SubscribeLocalEvent<CannonComponent, AmmoShotEvent>(AfterShot);
    }

    private void OnInit(EntityUid uid, CannonComponent cannon, ComponentInit args)
    {
        if (EntityManager.TryGetComponent<DeviceLinkSinkComponent>(uid, out var sink))
        {
            foreach (EntityUid sourceUid in sink.LinkedSources)
            {
                if (HasComp<CannonConsoleComponent>(sourceUid))
                {
                    cannon.BoundConsoleUid = sourceUid;
                    Dirty(uid, cannon);
                    break;
                }
            }
        }
    }

    private void OnRemoval(EntityUid uid, CannonComponent cannon, ComponentRemove args)
    {
        if (Exists(cannon.BoundLoaderUid) && TryComp<TurretLoaderComponent>(cannon.BoundLoaderUid, out var loader))
        {
            loader.BoundTurretUid = null;
            Dirty(cannon.BoundLoaderUid.Value, loader);
        }
    }

    private void OnRotateCannons(RotateCannonsEvent ev)
    {
        foreach (var uid in ev.Cannons)
        {
            var cannon = EntityManager.GetComponent<CannonComponent>(GetEntity(uid));
            if (!cannon.Rotatable)
                continue;

            _rotateToFaceSystem.TryFaceCoordinates(GetEntity(uid), ev.Coordinates);
        }
    }

    public void OnLink(EntityUid uid, CannonComponent cannon, NewLinkEvent ev)
    {
        if (ev.Sink != uid) //console is the source
            return;

        if (HasComp<CannonConsoleComponent>(ev.Source))
        {
            cannon.BoundConsoleUid = ev.Source;
            Dirty(uid, cannon);
        }
    }

    private void AfterShot(EntityUid entity, CannonComponent cannon, AmmoShotEvent args)
    {
        foreach (EntityUid projectile in args.FiredProjectiles)
        {
            var marker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(projectile);
            marker.Team = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(entity).Team;
        }

        if (cannon.BoundLoaderUid != null)
            RaiseLocalEvent(cannon.BoundLoaderUid.Value, new TurretLoaderAfterShotMessage());
    }

    public void RefreshFiringRanges(EntityUid uid, CannonComponent cannon)
    {
        cannon.ObstructedRanges = CalculateFiringRanges(uid, GetCannonGun(uid)!);
        Dirty(uid, cannon);
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
                if (ThetaHelpers.AngSectorsOverlap(start0, width0, start1, width1))
                {
                    (start2, width2) = ThetaHelpers.AngCombinedSector(start2, width2, start1, width1);
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
            ranges[i] = (ThetaHelpers.AngNormal(ranges[i].Item1 - ew), ranges[i].Item2 + ew);
        }

        return ranges;
    }

    private (Angle, Angle) GetDirSector(Vector2 dir)
    {
        Angle dirAngle = ThetaHelpers.AngNormal(new Angle(dir));
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

        Angle aangle = ThetaHelpers.AngNormal(new Angle(a));
        Angle bangle = ThetaHelpers.AngNormal(new Angle(b));
        Angle w = Angle.ShortestDistance(aangle, bangle);
        if (w < 0)
        {
            aangle = bangle;
            w = -w;
        }

        return (aangle, w);
    }
}
