using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Theta.ShipEvent.CircularShield;

public sealed class CircularShieldSlowdownEffect : CircularShieldEffect
{
    [Dependency] private EntityManager entMan = default!;
    [Dependency] private SharedTransformSystem formSys = default!;
    [Dependency] private SharedPhysicsSystem physSys = default!;

    [DataField("speedModifier", required: true)]
    public float SpeedModifier;
    
    public override void OnShieldInit(EntityUid uid, CircularShieldComponent shield)
    {
        IoCManager.InjectDependencies(this);
    }

    public override void OnShieldEnter(EntityUid uid, CircularShieldComponent shield)
    {
        TransformComponent form = entMan.GetComponent<TransformComponent>(uid);
        physSys.SetLinearVelocity(uid, physSys.GetLinearVelocity(uid, formSys.GetWorldPosition(form), xform: form)*SpeedModifier);
    }

    public override void OnShieldExit(EntityUid uid, CircularShieldComponent shield)
    {
        TransformComponent form = entMan.GetComponent<TransformComponent>(uid);
        physSys.SetLinearVelocity(uid, physSys.GetLinearVelocity(uid, formSys.GetWorldPosition(form), xform: form)*(1/SpeedModifier));
    }
}
