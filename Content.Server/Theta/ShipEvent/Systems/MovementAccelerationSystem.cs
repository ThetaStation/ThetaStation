using System.Numerics;
using Content.Server.Theta.ShipEvent.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class MovementAccelerationSystem : EntitySystem
{
    [Dependency] private readonly PhysicsSystem physSys = default!;
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach ((var body, var accel) in 
                 EntityManager.EntityQuery<PhysicsComponent, MovementAccelerationComponent>())
        {
            float v = Math.Min(accel.Acceleration * frameTime, accel.MaxVelocity);
            Vector2 vv = body.LinearVelocity.Normalized() * (v + body.LinearVelocity.Length());
            physSys.SetLinearVelocity(body.Owner, vv, body: body);
        }
    }
}
