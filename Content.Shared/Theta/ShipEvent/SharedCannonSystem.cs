using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Theta.ShipEvent;

public abstract class SharedCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestCannonShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopCannonShootEvent>(OnStopShootRequest);
    }

    private void OnShootRequest(RequestCannonShootEvent ev, EntitySessionEventArgs args)
    {
        var gun = _gunSystem.GetGun(ev.Cannon);
        if (gun == null || !_gunSystem.CanShoot(gun))
            return;

        var coords = EntityCoordinates.FromMap(ev.Cannon, new MapCoordinates(ev.Coordinates, Transform(ev.Cannon).MapID));
        _gunSystem.AttemptShoot(ev.Pilot, gun, coords);

    }

    private void OnStopShootRequest(RequestStopCannonShootEvent ev)
    {
        var gun = _gunSystem.GetGun(ev.Cannon);
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

