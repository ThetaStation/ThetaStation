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

        var query = EntityQueryEnumerator<TransformComponent, PhysicsComponent, MovementAccelerationComponent>();

        while (query.MoveNext(out var uid, out var form, out var body, out var accel))
        {
            if (body.LinearVelocity.Length() >= accel.MaxVelocity)
                return;

            float v = accel.Acceleration * frameTime;
            Vector2 vv = v * _formSys.GetWorldRotation(form).ToWorldVec();
            _physSys.SetLinearVelocity(uid, body.LinearVelocity + vv, body: body);
        }
    }
}
