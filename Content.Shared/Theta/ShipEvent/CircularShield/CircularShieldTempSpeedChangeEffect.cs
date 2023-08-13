using System.Numerics;
using Content.Shared.Projectiles;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Theta.ShipEvent.CircularShield;

public sealed class CircularShieldTempSpeedChangeEffect : CircularShieldEffect
{
    private IEntityManager entMan = default!;
    private SharedTransformSystem formSys = default!;
    private SharedPhysicsSystem physSys = default!;

    [DataField("speedModifier", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float SpeedModifier;

    [DataField("projectilesOnly"), ViewVariables(VVAccess.ReadWrite)]
    public bool ProjectilesOnly = true;

    public override void OnShieldInit(EntityUid uid, CircularShieldComponent shield)
    {
        entMan = IoCManager.Resolve<IEntityManager>();
        formSys = entMan.System<SharedTransformSystem>();
        physSys = entMan.System<SharedPhysicsSystem>();
    }

    public override void OnShieldEnter(EntityUid uid, CircularShieldComponent shield)
    {
        if (ProjectilesOnly && !entMan.HasComponent<ProjectileComponent>(uid))
            return;

        TransformComponent form = entMan.GetComponent<TransformComponent>(uid);
        physSys.SetLinearVelocity(uid, physSys.GetLinearVelocity(uid, formSys.GetWorldPosition(form), xform: form) * SpeedModifier);
    }

    public override void OnShieldExit(EntityUid uid, CircularShieldComponent shield)
    {
        if (ProjectilesOnly && !entMan.HasComponent<ProjectileComponent>(uid))
            return;

        TransformComponent form = entMan.GetComponent<TransformComponent>(uid);
        physSys.SetLinearVelocity(uid, physSys.GetLinearVelocity(uid, formSys.GetWorldPosition(form), xform: form) / SpeedModifier);
    }
}
