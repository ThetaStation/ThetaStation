using System.Linq;
using Content.Shared.CombatMode;
using Content.Shared.Physics;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent;

public abstract class SharedCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestCannonShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopCannonShootEvent>(OnStopShootRequest);
    }

    private void OnShootRequest(RequestCannonShootEvent ev, EntitySessionEventArgs args)
    {
        var gun = _gunSystem.GetGun(ev.Cannon);
        if (gun == null || !CanShoot(ev, gun))
        {
            return;
        }

        if (TryComp<SharedCombatModeComponent>(ev.Cannon, out var combatMode))
        {
            combatMode.IsInCombatMode = true;
        }

        var coords = EntityCoordinates.FromMap(ev.Cannon, new MapCoordinates(ev.Coordinates, Transform(ev.Cannon).MapID));
        _gunSystem.AttemptShoot(ev.Pilot, gun, coords);

    }

    public bool CanShoot(RequestCannonShootEvent args, GunComponent gun)
    {
        if (!_gunSystem.CanShoot(gun))
            return false;

        var cannonTransform = Transform(args.Cannon);
        var dir = args.Coordinates - cannonTransform.WorldPosition;
        var ray = new CollisionRay(cannonTransform.WorldPosition, dir.Normalized, (int) (CollisionGroup.Impassable));

        const int averageShipLength = 25;

        var rayCastResult = _physics.IntersectRay(cannonTransform.MapID, ray, averageShipLength).ToList();

        foreach (var result in rayCastResult)
        {
            if (Transform(result.HitEntity).GridUid == cannonTransform.GridUid)
                return false;
        }

        return true;
    }

    private void OnStopShootRequest(RequestStopCannonShootEvent ev)
    {
        var gun = _gunSystem.GetGun(ev.Cannon);
        if (gun == null || gun.ShotCounter == 0)
            return;

        if (TryComp<SharedCombatModeComponent>(ev.Cannon, out var combatMode))
        {
            combatMode.IsInCombatMode = false;
        }

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

