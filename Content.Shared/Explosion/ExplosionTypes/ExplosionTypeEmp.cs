using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Explosion.ExplosionTypes;

public sealed class ExplosionTypeEmp : ExplosionType
{

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
        if (damage == null || intensity == 0) { return; }
        foreach(IComponent component in _entityManager.GetComponents(entity))
        {
            if (component is IEmpable)
            {
                _entityManager.EventBus.RaiseLocalEvent(entity, new EmpEvent(intensity), false);
                break;
            }
        }
    }
}

