using Content.Shared.Damage;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Explosion.ExplosionTypes;

public sealed class ExplosionTypeStandard : ExplosionType
{
    private static ExplosionTypeStandard? _instance;

    public new static ExplosionType GetInstance()
    {
        if (_instance == null)
        {
            _instance = new ExplosionTypeStandard();
        }
        return _instance;
    }

    public override void ProcessEntity(
        EntityUid entity,
        MapCoordinates epicenter,
        DamageSpecifier? damage,
        float intensity,
        float throwForce,
        string explosionPrototypeId,
        EntityQuery<DamageableComponent> damageQuery,
        EntityQuery<PhysicsComponent> physicsQuery,
        TransformComponent? transform)
    {
        IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();
        DamageableSystem _damageableSystem = _entityManager.EntitySysManager.GetEntitySystem<DamageableSystem>();
        ThrowingSystem _throwingSystem = _entityManager.EntitySysManager.GetEntitySystem<ThrowingSystem>();
        // damage
        if (damage != null && damageQuery.TryGetComponent(entity, out var damageable))
        {
            var ev = new GetExplosionResistanceEvent(explosionPrototypeId);
            _entityManager.EventBus.RaiseLocalEvent(entity, ev, false);

            ev.DamageCoefficient = Math.Max(0, ev.DamageCoefficient);

            //todo need a way to track origin of explosion
            if (ev.DamageCoefficient == 1)
            {
                // no damage-dict multiplication required.
                _damageableSystem.TryChangeDamage(entity, damage, ignoreResistances: true, damageable: damageable);
            }
            else
            {
                _damageableSystem.TryChangeDamage(entity, damage * ev.DamageCoefficient, ignoreResistances: true, damageable: damageable);
            }
        }

        // throw
        if (transform != null
            && !transform.Anchored
            && throwForce > 0
            && !_entityManager.IsQueuedForDeletion(entity)
            && physicsQuery.TryGetComponent(entity, out var physics)
            && physics.BodyType == BodyType.Dynamic)
        {
            // TODO purge throw helpers and pass in physics component
            _throwingSystem.TryThrow(entity, transform.WorldPosition - epicenter.Position, throwForce);
        }
    }
}
