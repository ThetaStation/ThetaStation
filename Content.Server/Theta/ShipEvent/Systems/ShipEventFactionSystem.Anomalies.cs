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

namespace Content.Server.Theta.ShipEvent.Systems;

//todo: create one component for all kinds of anomalies, so it's behaviour depends on a list of effects instead of a specific comp
//also consider using same system for circular shields
public sealed partial class ShipEventFactionSystem
{
    [Dependency] private FixtureSystem _fixSys = default!;
    private const string AnomalyFixtureId = "ProxAnomalyFixture";

    public float AnomalyUpdateInterval;
    public float AnomalySpawnInterval;
    public List<EntityPrototype> AnomalyPrototypes = new();

    private void InitializeAnomalies()
    {
        SubscribeLocalEvent<ShipEventProximityAnomalyComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShipEventProximityAnomalyComponent, StartCollideEvent>(OnCollision);
    }

    private void OnInit(EntityUid uid, ShipEventProximityAnomalyComponent anomaly, ComponentInit args)
    {
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

    private void OnCollision(EntityUid uid, ShipEventProximityAnomalyComponent anomaly, StartCollideEvent args)
    {
        var gridUid = Transform(args.OtherEntity).GridUid;
        if (gridUid == null)
            return;
        anomaly.TrackedUids.Add(gridUid.Value);
    }

    private void AnomalyUpdate()
    {
        var query = EntityManager.EntityQueryEnumerator<ShipEventProximityAnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var anomaly, out var form))
        {
            Vector2 worldPos = _formSys.GetWorldPosition(form);

            if (IsPositionOutOfBounds(worldPos))
            {
                var body = Comp<PhysicsComponent>(uid);
                Vector2 normalDelta = (GetPlayAreaBounds().Center - worldPos).Normalized();
                _physSys.ApplyLinearImpulse(uid, normalDelta * 1000, body: body);
            }

            foreach (EntityUid trackedUid in anomaly.TrackedUids)
            {
                var trackedForm = Transform(trackedUid);
                //ik ik, proper way to do this would be getting grid's AABB and checking whether it's edge is still inside anomaly
                //but that's expensive and not very important
                if ((_formSys.GetWorldPosition(trackedForm) - _formSys.GetWorldPosition(form)).Length() > anomaly.Range + 10)
                {
                    anomaly.TrackedUids.Remove(trackedUid);
                    continue;
                }

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
