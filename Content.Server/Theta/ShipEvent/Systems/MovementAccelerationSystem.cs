using System.Numerics;
using Content.Server.Theta.ShipEvent.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class MovementAccelerationSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _formSys = default!;
    [Dependency] private readonly PhysicsSystem _physSys = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MovementAccelerationComponent, PhysicsComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var accel, out var body, out var form))
        {
            if (body.LinearVelocity.Length() >= accel.MaxVelocity)
                return;

            float v = accel.Acceleration * frameTime;
            Vector2 vv = v * _formSys.GetWorldRotation(form).ToWorldVec();
            _physSys.SetLinearVelocity(uid, body.LinearVelocity + vv, body: body);
        }
    }
}
