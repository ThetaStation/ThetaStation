using System.Linq;
using System.Numerics;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;

//todo: create one component for all kinds of anomalies, so it's behaviour depends on a list of effects instead of a specific comp
//also consider using same system for circular shields
public sealed partial class ShipEventFactionSystem
{
    public float AnomalyUpdateInterval;
    public float AnomalySpawnInterval;
    public List<EntityPrototype> AnomalyPrototypes = new();

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
                _physSys.ResetDynamics(body);
                _physSys.SetLinearVelocity(uid, normalDelta * 10, body: body);
                _physSys.ApplyLinearImpulse(uid, normalDelta * 10, body: body);
            }

            //todo
            //single check against whole anomaly range is unreliable and often does not include player ships (idk why)
            //so instead we do 4 checks. obviously it's also 4 times more expensive
            Vector2 lb = worldPos + new Vector2(-anomaly.Range);
            Vector2 rb = worldPos + new Vector2(anomaly.Range, -anomaly.Range);
            Vector2 lt = worldPos + new Vector2(-anomaly.Range, anomaly.Range);
            Vector2 rt = worldPos + new Vector2(anomaly.Range);

            HashSet<MapGridComponent> intersections = new();
            intersections.UnionWith(_mapMan.FindGridsIntersecting(TargetMap, Box2.FromTwoPoints(worldPos, lb)));
            intersections.UnionWith(_mapMan.FindGridsIntersecting(TargetMap, Box2.FromTwoPoints(worldPos, rb)));
            intersections.UnionWith(_mapMan.FindGridsIntersecting(TargetMap, Box2.FromTwoPoints(worldPos, lt)));
            intersections.UnionWith(_mapMan.FindGridsIntersecting(TargetMap, Box2.FromTwoPoints(worldPos, rt)));

            foreach (var grid in intersections)
            {
                if (!TryComp<ShipEventFactionMarkerComponent>(grid.Owner, out var marker))
                    return;

                var gridForm = Transform(grid.Owner);
                SpawnAtPosition(anomaly.ToSpawn, Transform(Pick(gridForm.ChildEntities)).Coordinates);
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
