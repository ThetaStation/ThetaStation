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
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Server.Theta.ShipEvent;

public sealed class CannonSystem : SharedCannonSystem
{
    [Dependency] private readonly TransformSystem _formSys = default!;
    [Dependency] private readonly PhysicsSystem _physSys = default!;
    [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;

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

    private void AfterShot(EntityUid uid, CannonComponent cannon, AmmoShotEvent args)
    {
        if (cannon.Recoil > 0)
        {
            var cannonForm = Transform(uid);
            _physSys.ApplyLinearImpulse(
                cannonForm.GridUid!.Value,
                _formSys.GetWorldRotation(cannonForm).ToWorldVec() * cannon.Recoil * args.FiredProjectiles.Count * -1
                );
        }

        foreach (EntityUid projectile in args.FiredProjectiles)
        {
            var marker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(projectile);
            marker.Team = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(uid).Team;
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

        foreach (EntityUid childUid in gridForm.ChildEntities)
        {
            //checking if obstacle is not too far/close to the cannon
            TransformComponent form = Transform(childUid);
            Vector2 dir = form.LocalPosition - Transform(uid).LocalPosition;
            float dist = dir.Length();
            if (dist > CollisionCheckDistance || dist < 1)
                continue;

            //checking that obstacle is anchored and solid
            PhysicsComponent? phys = EntityManager.GetComponentOrNull<PhysicsComponent>(childUid);
            if (!form.Anchored || phys == null || !phys.Hard)
                continue;

            //calculating circular sector that obstacle occupies relative to the cannon
            (Angle start0, Angle width0) = GetObstacleSector(dir);
            ranges.Add((start0, width0));

            (Angle start2, Angle width2) = (start0, width0);

            //checking whether new sector overlaps with any existing ones and combining them if so
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

        Angle maxSpread = Angle.Zero;

        //adding ammo spread (for shotguns)
        CannonComponent cannon = Comp<CannonComponent>(uid);
        foreach (string ammoProtId in cannon.AmmoPrototypes)
        {
            EntityPrototype ammoProt = _protMan.Index<EntityPrototype>(ammoProtId);
            if (ammoProt.Components.TryGetValue("CartridgeAmmo", out var compEntry)
                && compEntry.Component is CartridgeAmmoComponent cartridge)
            {
                maxSpread = cartridge.Spread > maxSpread ? cartridge.Spread : maxSpread;
            }
        }

        //and spread from the gun itself
        maxSpread += gun.MaxAngle + Angle.FromDegrees(10);

        //subtracting spread from every range
        for (int i = 0; i < ranges.Count; i++)
        {
            ranges[i] = (ThetaHelpers.AngNormal(ranges[i].Item1 - maxSpread / 2), ranges[i].Item2 + maxSpread);
        }

        return ranges;
    }

    //delta between cannon's and obstacle's position
    private (Angle, Angle) GetObstacleSector(Vector2 delta)
    {
        Angle dirAngle = ThetaHelpers.AngNormal(new Angle(delta));
        Vector2 a, b;

        //this can be done without ugly conditional below, by rotating tile's square by delta's angle and finding left- and rightmost points,
        //but this certainly will be heavier and less clear
        if (dirAngle % (Math.PI * 0.5) == 0)
        {
            switch (dirAngle.Theta)
            {
                case 0:
                case Math.Tau:
                    a = delta - Vector2Helpers.Half;
                    b = new Vector2(delta.X - 0.5f, delta.Y + 0.5f);
                    break;
                case Math.PI * 0.5:
                    a = delta - Vector2Helpers.Half;
                    b = new Vector2(delta.X + 0.5f, delta.Y - 0.5f);
                    break;
                case Math.PI:
                    a = delta + Vector2Helpers.Half;
                    b = new Vector2(delta.X + 0.5f, delta.Y - 0.5f);
                    break;
                case Math.PI * 1.5:
                    a = delta + Vector2Helpers.Half;
                    b = new Vector2(delta.X - 0.5f, delta.Y + 0.5f);
                    break;
                default:
                    return (double.NaN, double.NaN);
            }
        }
        else if (dirAngle > 0 && dirAngle < Math.PI * 0.5 || dirAngle > Math.PI && dirAngle < Math.PI * 1.5)
        {
            a = new Vector2(delta.X - 0.5f, delta.Y + 0.5f);
            b = new Vector2(delta.X + 0.5f, delta.Y - 0.5f);
        }
        else
        {
            a = delta + Vector2Helpers.Half;
            b = delta - Vector2Helpers.Half;
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
