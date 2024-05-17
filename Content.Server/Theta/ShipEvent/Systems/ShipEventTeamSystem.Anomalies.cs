using System.Numerics;
using Content.Shared.Physics;
using Content.Server.Theta.ShipEvent.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics;

namespace Content.Server.Theta.ShipEvent.Systems;

//todo: create one component for all kinds of anomalies, so it's behaviour depends on a list of effects instead of a specific comp
//also consider using same system for circular shields
public sealed partial class ShipEventTeamSystem
{
    [Dependency] private FixtureSystem _fixSys = default!;
    private const string AnomalyFixtureId = "ProxAnomalyFixture";

    public float AnomalyUpdateInterval;
    public float AnomalySpawnInterval;
    public List<EntityPrototype> AnomalyPrototypes = new();

    private void InitializeAnomalies()
    {
        SubscribeLocalEvent<ShipEventProximityAnomalyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShipEventProximityAnomalyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShipEventProximityAnomalyComponent, StartCollideEvent>(OnCollision);
        SubscribeLocalEvent<ShipEventProximityAnomalyComponent, EndCollideEvent>(OnExit);
    }

    private void OnInit(EntityUid uid, ShipEventProximityAnomalyComponent anomaly, ComponentInit args)
    {
        if (!HasComp<FixturesComponent>(uid))
            return;
        Fixture? fix = _fixSys.GetFixtureOrNull(uid, AnomalyFixtureId);

        if (fix == null)
        {
            PhysShapeCircle circle = new(anomaly.Range);
            _fixSys.TryCreateFixture(uid, circle, AnomalyFixtureId, hard: false, collisionLayer: (int) CollisionGroup.Impassable);
        }
        else
        {
            _physSys.SetRadius(uid, AnomalyFixtureId, fix, fix.Shape, anomaly.Range);
        }
    }

    private void OnShutdown(EntityUid uid, ShipEventProximityAnomalyComponent anomaly, ComponentShutdown args)
    {
        var query = EntityManager.EntityQueryEnumerator<ShipEventProximityAnomalyTrackerComponent>();
        while (query.MoveNext(out var trackedUid, out var tracker))
        {
            if (tracker.TrackedBy == uid)
                EntityManager.RemoveComponent<ShipEventProximityAnomalyTrackerComponent>(trackedUid);
        }
    }

    private void OnCollision(EntityUid uid, ShipEventProximityAnomalyComponent anomaly, ref StartCollideEvent args)
    {
        var gridUid = Transform(args.OtherEntity).GridUid;
        if (gridUid == null)
            return;

        EnsureComp<ShipEventProximityAnomalyTrackerComponent>(gridUid.Value).TrackedBy = uid;
    }

    private void OnExit(EntityUid uid, ShipEventProximityAnomalyComponent anomaly, ref EndCollideEvent args)
    {
        var gridUid = Transform(args.OtherEntity).GridUid;
        if (gridUid == null)
            return;

        EntityManager.RemoveComponent<ShipEventProximityAnomalyTrackerComponent>(gridUid.Value);
    }

    private void AnomalyUpdate()
    {
        var query = EntityManager.EntityQueryEnumerator<ShipEventProximityAnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var anomaly, out var form))
        {
            Vector2 worldPos = _formSys.GetWorldPosition(form);

            if (IsPositionOutOfBounds(worldPos))
                _formSys.SetWorldPosition(form, GetPlayAreaBounds().Center);

            var trackerQuery = EntityManager.EntityQueryEnumerator<ShipEventProximityAnomalyTrackerComponent>();
            while (trackerQuery.MoveNext(out var trackedUid, out var tracker))
            {
                if (tracker.TrackedBy != uid)
                    continue;

                var trackedForm = Transform(trackedUid);
                SpawnAtPosition(anomaly.ToSpawn, Transform(Pick(trackedForm.ChildEntities)).Coordinates);
            }
        }
    }

    private void AnomalySpawn()
    {
        if (AnomalyPrototypes.Count == 0)
            return;

        RandomPosEntSpawn(Pick(AnomalyPrototypes).ID, 50);
    }
}
