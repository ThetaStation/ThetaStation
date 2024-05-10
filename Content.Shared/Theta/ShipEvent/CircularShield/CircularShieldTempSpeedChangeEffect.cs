using Content.Shared.Projectiles;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Theta.ShipEvent.CircularShield;

public sealed partial class CircularShieldTempSpeedChangeEffect : CircularShieldEffect
{
    private IEntityManager _entMan = default!;
    private SharedTransformSystem _formSys = default!;
    private SharedPhysicsSystem _physSys = default!;
    private SharedCircularShieldSystem _shieldSys = default!;

    [DataField("speedModifier", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float SpeedModifier;

    [DataField("projectilesOnly"), ViewVariables(VVAccess.ReadWrite)]
    public bool ProjectilesOnly = true;

    private HashSet<EntityUid> _trackedUids = new();

    public override void OnShieldInit(EntityUid uid, CircularShieldComponent shield)
    {
        _entMan = IoCManager.Resolve<IEntityManager>();
        _formSys = _entMan.System<SharedTransformSystem>();
        _physSys = _entMan.System<SharedPhysicsSystem>();
        _shieldSys = _entMan.System<SharedCircularShieldSystem>();
    }

    public override void OnShieldShutdown(EntityUid uid, CircularShieldComponent shield)
    {
        foreach (EntityUid id in _trackedUids)
        {
            RestoreVelocity(uid);
        }

        _trackedUids.Clear();
    }

    public override void OnShieldUpdate(EntityUid uid, CircularShieldComponent shield, float time)
    {
        base.OnShieldUpdate(uid, shield, time);
        if (_trackedUids.Count == 0)
            return;

        foreach (EntityUid trackedUid in _trackedUids)
        {
            if (!_entMan.EntityExists(trackedUid))
            {
                _trackedUids.Remove(trackedUid);
                continue;
            }

            if (!_shieldSys.EntityInShield(uid, shield, trackedUid, _formSys))
            {
                RestoreVelocity(trackedUid);
                _trackedUids.Remove(trackedUid);
            }
        }
    }

    public override void OnShieldEnter(EntityUid uid, CircularShieldComponent shield)
    {
        if (ProjectilesOnly && !_entMan.HasComponent<ProjectileComponent>(uid))
            return;

        TransformComponent form = _entMan.GetComponent<TransformComponent>(uid);
        _physSys.SetLinearVelocity(uid, _physSys.GetLinearVelocity(uid, _formSys.GetWorldPosition(form), xform: form) * SpeedModifier);
        _trackedUids.Add(uid);
    }

    private void RestoreVelocity(EntityUid uid)
    {
        if (!_entMan.EntityExists(uid))
            return;
        TransformComponent form = _entMan.GetComponent<TransformComponent>(uid);
        _physSys.SetLinearVelocity(uid, _physSys.GetLinearVelocity(uid, _formSys.GetWorldPosition(form), xform: form) / SpeedModifier);
    }
}
