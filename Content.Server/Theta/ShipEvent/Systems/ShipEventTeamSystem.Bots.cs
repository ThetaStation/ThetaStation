using Content.Server.Physics.Controllers;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Construction;
using Content.Shared.Theta;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static Content.Shared.Theta.ThetaHelpers;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventTeamSystem
{
    [Dependency] private readonly MoverController _moveControl = default!;
    [Dependency] private readonly CannonSystem _cannonSys = default!;

    public float BotUpdateInterval;
    public int BotAmount;

    //braking input = this value / distance to waypoint
    public const int BotBrakingSpeed = 15;

    //after moving to a waypoint closer that that distance it will move onto the next one
    public const int BotMinWaypointDist = 5;

    //how close bot will try to get to it's target when attacking
    //so on 1 it will ram straight into it, at 0.5 it will only come half way, etc.
    public const float BotAttackApproachPercent = 0.8f;

    private void BotInit()
    {
        SubscribeLocalEvent<ShipEventBotComponent, StartCollideEvent>(OnBotCollide);
    }

    private void OnBotCollide(EntityUid uid, ShipEventBotComponent bot, ref StartCollideEvent args)
    {
        if ((_timingSys.CurTime - bot.LastUpdate).TotalSeconds > 1)
            BotUpdatePath(uid, bot);
    }

    //move & shoot according to current path, called as frequently as possible
    private void BotUpdateMovementAll(float frameTime)
    {
        var query = EntityQueryEnumerator<TransformComponent, ShuttleComponent, PhysicsComponent, ShipEventBotComponent>();
        var formQuery = GetEntityQuery<TransformComponent>();
        while (query.MoveNext(out var uid, out var form, out var shuttle, out var body, out var bot))
        {
            BotUpdateMovement(frameTime, uid, form, shuttle, body, bot, formQuery);
        }
    }

    //quite expensive, so only called once per BotUpdateInterval
    private void BotUpdatePathAll()
    {
        var query = EntityQueryEnumerator<ShipEventBotComponent>();
        while (query.MoveNext(out var uid, out var bot))
        {
            BotUpdatePath(uid, bot);
        }
    }

    private void BotUpdateMovement(
        float frameTime,
        EntityUid uid,
        TransformComponent form,
        ShuttleComponent shuttle,
        PhysicsComponent body,
        ShipEventBotComponent bot,
        EntityQuery<TransformComponent> formQuery) //was used by mover controller's code so I've left it as is
    {
        //move
        if (bot.Waypoints.Count == 0)
            return;

        float distToTarget = Math.Max((bot.Waypoints[bot.CurrentWaypoint] - form.LocalPosition).Length(), 0.1f);
        float brakeInput = Math.Max(BotBrakingSpeed / distToTarget, 0.1f);

        if (distToTarget < BotMinWaypointDist)
            bot.CurrentWaypoint++;

        Vector2 delta = bot.Waypoints[bot.CurrentWaypoint];
        _moveControl.MoveShuttle(frameTime, uid, shuttle, body, delta, (float) delta.ToWorldAngle(), brakeInput, formQuery);

        //shoot
        if (bot.TargetUid == null || Deleted(bot.TargetUid))
            return;

        TransformComponent targetForm = formQuery.GetComponent(bot.TargetUid.Value);
        distToTarget = (targetForm.LocalPosition - form.LocalPosition).Length();
        if (distToTarget < bot.MaxAttackRange)
            BotFireWeapons(uid, bot, targetForm.LocalPosition);
    }

    private void BotFireWeapons(EntityUid uid, ShipEventBotComponent bot, Vector2 coords)
    {
        foreach (EntityUid cannonUid in GetGridCompHolders<CannonComponent>(uid))
        {
            _cannonSys.ShootCannon(cannonUid, cannonUid, coords);
        }
    }

    private void BotUpdatePath(EntityUid uid, ShipEventBotComponent bot)
    {
        bot.LastUpdate = _timingSys.CurTime;
        TransformComponent form = Transform(uid);
        Box2 pathfindingBox = new Box2(form.LocalPosition - new Vector2(bot.PathfindingRange), form.LocalPosition + new Vector2(bot.PathfindingRange));

        Vector2 targetPoint = Vector2.Zero;
        //determine where bot wants to go
        switch (bot.State)
        {
            case BotState.Wander: //some random point
                {
                    targetPoint = _random.NextVector2Box(pathfindingBox.Left, pathfindingBox.Bottom, pathfindingBox.Right, pathfindingBox.Top);
                    break;
                }
            case BotState.Attack: //as close to our target as possible
                {
                    if (BotCheckTarget(uid, bot, form, out Vector2? pos))
                        targetPoint = pos.Value;
                    break;
                }
            case BotState.Avoid: //as far from our target as possible
                {
                    if (BotCheckTarget(uid, bot, form, out Vector2? pos))
                        targetPoint = 2 * form.LocalPosition - pos.Value;
                    break;
                }
        }

        //find nearby grids & check for foes
        float minFoeDistance = 0;
        List<Entity<MapGridComponent>> grids = new();
        _mapMan.FindGridsIntersecting(TargetMap, pathfindingBox, ref grids, includeMap: false);

        if (bot.State != BotState.Avoid)
        {
            foreach ((EntityUid gridUid, MapGridComponent grid) in grids)
            {
                if (HasComp<ShipEventTeamMarkerComponent>(gridUid))
                {
                    float dist = (Transform(gridUid).LocalPosition - form.LocalPosition).Length();
                    if (dist < minFoeDistance && dist < bot.MaxFollowRange)
                    {
                        minFoeDistance = dist;
                        bot.TargetUid = gridUid;
                        bot.State = BotState.Attack;
                    }
                }
            }
        }

        //finally, pathfind
        targetPoint = new Vector2(
            Math.Clamp(targetPoint.X, pathfindingBox.Left, pathfindingBox.Right),
            Math.Clamp(targetPoint.Y, pathfindingBox.Bottom, pathfindingBox.Top));
    }

    //returns true if target is valid and bot should continue following/avoiding it
    private bool BotCheckTarget(
        EntityUid uid,
        ShipEventBotComponent? bot,
        TransformComponent? form,
        [NotNullWhen(true)] out Vector2? pos)
    {
        pos = null;

        if (!Resolve(uid, ref bot) || !Resolve(uid, ref form))
            return false;

        if (bot.TargetUid == null || Deleted(bot.TargetUid))
        {
            bot.TargetUid = null;
            bot.State = BotState.Wander;
            return false;
        }

        TransformComponent targetForm = Transform(bot.TargetUid.Value);
        if ((targetForm.LocalPosition - form.LocalPosition).Length() > bot.MaxFollowRange)
        {
            bot.TargetUid = null;
            bot.State = BotState.Wander;
            return false;
        }

        pos = targetForm.LocalPosition;
        return true;
    }

    #region Pathfinding
    //adapting algorithm used by NPC pathfinding turned out to be too much work, so reinventing the bicycle here
    //general idea is to unify AABBs of obstacles, turn each vertex into a graph node
    //then try to connect start and end node to all available nodes by casting a bunch of rays & checking if any AABBs intersect em
    //finally run Djikstra's algorithm to find the shortest path in the result graph

    /// <summary>
    /// Returns root nodes of the resulting graphs
    /// Graph search algorithm can then be run on it in order to find the actual route
    /// </summary>
    private List<GraphNode<Vector2>> BuildGraph(List<Entity<MapGridComponent>> grids, Vector2 start, Vector2 finish)
    {
        List<RectRange> ranges = [];
        foreach ((EntityUid uid, MapGridComponent grid) in grids)
        {
            Box2 worldAabb = _formSys.GetWorldMatrix(uid).TransformBox(grid.LocalAABB);
            ranges.Add(RangeFromBox((Box2i) worldAabb));
        }

        return RangesToGraph(ranges);
    }

    #endregion
}