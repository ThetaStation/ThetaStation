using System.Numerics;
using Content.Server.Theta.ShipEvent.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class MovementAccelerationSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem formSys = default!;
    [Dependency] private readonly PhysicsSystem physSys = default!;
    
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach ((var form, var body, var accel) in 
                 EntityManager.EntityQuery<TransformComponent, PhysicsComponent, MovementAccelerationComponent>())
        {
            if (body.LinearVelocity.Length() >= accel.MaxVelocity)
                return;
            
            float v = accel.Acceleration * frameTime;
            Vector2 vv = v * formSys.GetWorldRotation(form).ToWorldVec();
            physSys.SetLinearVelocity(body.Owner, body.LinearVelocity + vv, body: body);
        }
    }
}
