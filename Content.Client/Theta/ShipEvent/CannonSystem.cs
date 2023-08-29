using System.Numerics;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Theta.ShipEvent;

public sealed class CannonSystem : SharedCannonSystem
{
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, Vector2> _toUpdateRotation = new();

    private readonly Dictionary<EntityUid, (EntityUid, Vector2)> _firingCannons = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<CannonComponent, RotateCannonEvent>(RotateCannons);
        SubscribeLocalEvent<CannonComponent, StartCannonFiringEvent>(RequestCannonShoot);
        SubscribeLocalEvent<CannonComponent, StopCannonFiringEventEvent>(RequestStopCannonShoot);
        SubscribeLocalEvent<CannonComponent, ComponentRemove>(OnComponentRemove);
    }

    private void RequestCannonShoot(EntityUid uid, CannonComponent cannon, ref StartCannonFiringEvent args)
    {
        _firingCannons[uid] = (args.Pilot, args.Coordinates);
    }

    private void OnComponentRemove(EntityUid uid, CannonComponent cannon, ComponentRemove args)
    {
        _firingCannons.Remove(uid);
    }

    private void RequestStopCannonShoot(EntityUid uid, CannonComponent cannon, ref StopCannonFiringEventEvent args)
    {
        if (_firingCannons.Remove(uid))
        {
            RaisePredictiveEvent(new RequestStopCannonShootEvent
            {
                CannonUid = uid,
            });
        }
    }

    protected override void OnAnchorChanged(EntityUid uid, CannonComponent cannon, ref AnchorStateChangedEvent args)
    {
        base.OnAnchorChanged(uid, cannon, ref args);

        if (!args.Anchored)
            _firingCannons.Remove(uid);
    }

    private void RotateCannons(EntityUid uid, CannonComponent cannon, ref RotateCannonEvent args)
    {
        /*if (!cannon.Rotatable)
            return;*/

        _toUpdateRotation[uid] = args.Coordinates;
        UpdateCoordinates(uid, args.Coordinates, args.Pilot, cannon);
    }

    private void UpdateCoordinates(EntityUid uid, Vector2 coords, EntityUid pilot, CannonComponent cannon)
    {
        if (!_firingCannons.ContainsKey(uid))
            return;
        _firingCannons[uid] = (pilot, coords);
    }

    public override void Update(float frameTime)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var aggregateByVector = new Dictionary<Vector2, List<EntityUid>>();
        foreach (var (uid, vector2) in _toUpdateRotation)
        {
            // https://github.com/space-wizards/space-station-14/issues/11446
            // some kind of prediction bug for the client
            //foreach (var uid in list)
            //{
            //    RotateToFaceSystem.TryFaceCoordinates(uid, coordinates);
            //}
            var list = aggregateByVector.GetOrNew(vector2);
            list.Add(uid);
        }

        foreach (var (coordinates, list) in aggregateByVector)
        {
            RaiseNetworkEvent(new RotateCannonsEvent(coordinates, list));
        }

        Firing();

        _toUpdateRotation.Clear();
    }

    private void Firing()
    {
        foreach (var (uid, (pilot, vector2)) in _firingCannons)
        {
            if (!CanFire(uid))
                return;

            RaisePredictiveEvent(new RequestCannonShootEvent
            {
                CannonUid = uid,
                Coordinates = vector2,
                PilotUid = pilot
            });
        }
    }

    private bool CanFire(EntityUid cannonUid)
    {
        var gun = GetCannonGun(cannonUid);
        if (gun == null)
        {
            _firingCannons.Remove(cannonUid);
            return false;
        }

        if (!_gunSystem.CanShoot(gun))
        {
            return false;
        }

        return true;
    }
}

[ByRefEvent]
public readonly struct StartCannonFiringEvent
{
    public readonly Vector2 Coordinates;

    public readonly EntityUid Pilot;

    public StartCannonFiringEvent(Vector2 coordinates, EntityUid pilot)
    {
        Coordinates = coordinates;
        Pilot = pilot;
    }
}

[ByRefEvent]
public readonly struct StopCannonFiringEventEvent
{
}

[ByRefEvent]
public readonly struct RotateCannonEvent
{
    public readonly Vector2 Coordinates;

    public readonly EntityUid Pilot;

    public RotateCannonEvent(Vector2 coordinates, EntityUid pilot)
    {
        Coordinates = coordinates;
        Pilot = pilot;
    }
}
