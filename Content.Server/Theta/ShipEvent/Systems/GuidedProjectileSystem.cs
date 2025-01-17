using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Theta.ShipEvent.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class GuidedProjectileSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _formSys = default!;
    [Dependency] private readonly PhysicsSystem _physSys = default!;
    [Dependency] private readonly ExplosionSystem _expSys = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<GuidedProjectileComponent>();
        while (query.MoveNext(out var uid, out var proj))
        {
            if (proj.Waypoints == null)
                continue;

            if (proj.NextCourseUpdate <= _timing.CurTime)
                UpdateCourse(uid, proj);
        }
    }

    private void UpdateCourse(EntityUid uid, GuidedProjectileComponent proj)
    {
        if (proj.CurrentWaypoint == proj.Waypoints.Count - 1)
        {
            proj.NextCourseUpdate = TimeSpan.MaxValue;
            _expSys.TriggerExplosive(uid);
            return;
        }

        Vector2 delta = proj.Waypoints[proj.CurrentWaypoint + 1] - proj.Waypoints[proj.CurrentWaypoint];
        Angle worldRot = Angle.FromWorldVec(delta);
        _formSys.SetWorldRotation(uid, worldRot);
        _physSys.SetLinearVelocity(uid, delta.Normalized() * proj.Velocity);
        proj.CurrentWaypoint++;

        TimeSpan eta = TimeSpan.FromSeconds(delta.Length() / _physSys.GetLinearVelocity(uid, proj.Waypoints[proj.CurrentWaypoint]).Length());
        proj.NextCourseUpdate = _timing.CurTime + eta;
    }
}