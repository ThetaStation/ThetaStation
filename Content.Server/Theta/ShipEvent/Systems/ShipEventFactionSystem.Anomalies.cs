using System.Numerics;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Theta.ShipEvent.Systems;

//todo: create one component for all kinds of anomalies, so it's behaviour depends on a list of effects instead of a specific comp
//also consider using same system for circular shields
public sealed partial class ShipEventFactionSystem
{
    public float AnomalyUpdateInterval;
    public float AnomalySpawnInterval;
    public List<EntityPrototype> AnomalyPrototypes = new();

    private const int WormholeEffectDuration = 10; //seconds
    private HashSet<EntityUid> MovedByWormhole = new();

    private void AnomalyInit()
    {
        SubscribeLocalEvent<ShipEventWormholeSpawnerComponent, ComponentInit>(OnWormholeSpawnerInit);
        SubscribeLocalEvent<ShipEventWormholeAnomalyComponent, ComponentInit>(OnWormholeInit);
        SubscribeLocalEvent<ShipEventWormholeAnomalyComponent, ComponentRemove>(OnWormholeRemoved);
        SubscribeLocalEvent<ShipEventWormholeAnomalyComponent, EntParentChangedMessage>(OnWormholeParentChange);
        SubscribeLocalEvent<ShipEventWormholeAnomalyComponent, StartCollideEvent>(OnWormholeCollide);
    }

    private void OnWormholeRemoved(EntityUid uid, ShipEventWormholeAnomalyComponent component, ComponentRemove args)
    {
        throw new NotImplementedException();
    }

    private void OnWormholeSpawnerInit(EntityUid uid, ShipEventWormholeSpawnerComponent spawner, ComponentInit args)
    {
        EntityUid? uid1 = RandomPosEntSpawn(spawner.WormholeProtoId, 50);
        if (uid1 == null)
            return;

        EntityUid? uid2 = null;
        for (int c = 0; c < 50; c++)
        {
            Vector2 worldPos = _random.NextAngle().ToWorldVec() * _random.Next(spawner.MinDistance, spawner.MaxDistance);
            if (IsPositionOutOfBounds(worldPos) || _mapMan.TryFindGridAt(TargetMap, worldPos, out _, out _))
                continue;
            uid2 = SpawnAtPosition(spawner.WormholeProtoId, new EntityCoordinates(_mapMan.GetMapEntityId(TargetMap), worldPos));
            break;
        }

        if (uid2 == null)
        {
            Del(uid1);
            return;
        }

        Comp<ShipEventWormholeAnomalyComponent>(uid1.Value).BoundWormhole = uid2;
        Comp<ShipEventWormholeAnomalyComponent>(uid2.Value).BoundWormhole = uid1;
        QueueDel(uid);
    }

    private void OnWormholeInit(EntityUid uid, ShipEventWormholeAnomalyComponent wormhole, ComponentInit args)
    {
        wormhole.OriginalPosition = _formSys.GetWorldPosition(uid);
    }

    //todo: this is hot garbage
    //it's currently impossible to create entity which collides with grids without being a grid itself
    //also impossible to create entity which collides with walls and other stuff, without traversing grids
    //so in this method after traversing grids we snap back to original position, detach back to space and start teleportation sequence
    private void OnWormholeParentChange(EntityUid uid, ShipEventWormholeAnomalyComponent wormhole, EntParentChangedMessage args)
    {
        var form = Transform(uid);
        if (wormhole.BoundWormhole == null ||
            args.OldParent != _mapMan.GetMapEntityId(TargetMap) ||
            args.OldParent == null && form.GridUid == null)
            return;

        //so we don't reparent wormhole to grid again
        Vector2 gridWorldPos = _formSys.GetWorldPosition(form.ParentUid);
        Vector2 normalDelta = (wormhole.OriginalPosition - gridWorldPos).Normalized();
        _formSys.SetWorldPosition(form.ParentUid, gridWorldPos - normalDelta * 2);

        WormholeMove(form.ParentUid, wormhole.BoundWormhole.Value, wormhole.OriginalPosition);
        _formSys.SetWorldPosition(uid, wormhole.OriginalPosition);
        _formSys.SetParent(uid, _mapMan.GetMapEntityId(TargetMap));
    }

    private void OnWormholeCollide(EntityUid uid, ShipEventWormholeAnomalyComponent wormhole, StartCollideEvent args)
    {
        var form = Transform(args.OtherEntity);

        if (wormhole.BoundWormhole == null || form.GridUid == null)
            return;

        WormholeMove(form.GridUid.Value, wormhole.BoundWormhole.Value, args.WorldPoint);
    }

    private void WormholeMove(EntityUid targetUid, EntityUid wormholeUid, Vector2 collisionPoint)
    {
        if (MovedByWormhole.Contains(targetUid))
            return;
        MovedByWormhole.Add(targetUid);

        var netUid = GetNetEntity(targetUid);
        var targetBody = Comp<PhysicsComponent>(targetUid);
        var holeForm = Transform(wormholeUid);

        if ((targetBody.BodyType & Robust.Shared.Physics.BodyType.Dynamic) == 0)
            return;

        //apply shrinking effect, lock movement
        _physSys.SetBodyType(targetUid, Robust.Shared.Physics.BodyType.Static);
        WormholeOverlayAddGrid addEv = new()
        {
            GridUid = netUid,
            Reverse = false,
            AttractionCenter = collisionPoint
        };
        RaiseNetworkEvent(addEv);

        //wait until it shrinks completely
        Timer.Spawn(WormholeEffectDuration * 1000, () =>
        {
            //actually teleport the ship
            Vector2 holeWorldPos = _formSys.GetWorldPosition(holeForm);
            Vector2 normalDelta = (holeWorldPos - _formSys.GetWorldPosition(targetUid)).Normalized();
            _formSys.SetWorldRotation(targetUid, Angle.Zero);
            _formSys.SetWorldPosition(targetUid, holeWorldPos - normalDelta * 2);

            //apply expanding effect
            WormholeOverlayAddGrid addEv = new()
            {
                GridUid = netUid,
                Reverse = true,
                AttractionCenter = collisionPoint
            };
            RaiseNetworkEvent(addEv);

            //wait until it fully expands, unlock movement and remove the effect
            Timer.Spawn(WormholeEffectDuration * 1000, () =>
            {
                MovedByWormhole.Remove(targetUid);
                _physSys.SetBodyType(targetUid, Robust.Shared.Physics.BodyType.Dynamic);
                WormholeOverlayRemoveGrid removeEv = new()
                {
                    GridUid = netUid
                };
                RaiseNetworkEvent(removeEv);
            });
        });
    }

    private void AnomalyUpdate()
    {
        var query = EntityManager.EntityQueryEnumerator<ShipEventProximityAnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var anomaly, out var form))
        {
            Vector2 worldPos = _formSys.GetWorldPosition(form);
            Vector2 bottomLeft = worldPos - new Vector2(anomaly.Range / 2);
            Vector2 topRight = worldPos + new Vector2(anomaly.Range / 2);

            if (IsPositionOutOfBounds(worldPos))
            {
                Vector2 delta = GetPlayAreaBounds().Center - worldPos / 2;
                _physSys.SetLinearVelocity(uid, delta);
                _physSys.ApplyLinearImpulse(uid, delta);
            }

            foreach (var grid in _mapMan.FindGridsIntersecting(TargetMap, new Box2(bottomLeft, topRight), true))
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