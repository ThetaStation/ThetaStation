using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Theta.ShipEvent.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class VelocityExplosionTriggerSystem : EntitySystem
{
    [Dependency] private readonly ExplosionSystem _expSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VelocityExplosionTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, VelocityExplosionTriggerComponent trigger, TriggerEvent args)
    {
        if (TryComp<ExplosiveComponent>(uid, out var exp) && TryComp<PhysicsComponent>(uid, out var body))
        {
            float v = body.LinearVelocity.Length();
            if (v < trigger.MinimumVelocity)
                return;

            _expSys.TriggerExplosive(uid, exp, totalIntensity: Math.Min(trigger.IntensityMultiplier * v, trigger.MaximumIntensity));
        }
    }
}
