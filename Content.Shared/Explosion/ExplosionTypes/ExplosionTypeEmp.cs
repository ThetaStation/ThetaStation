using System.Runtime.CompilerServices;
using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Explosion.ExplosionTypes;

public sealed class ExplosionTypeEmp : ExplosionType
{
    private static ExplosionTypeEmp? _instance;

    public new static ExplosionType GetInstance()
    {
        if (_instance == null)
        {
            _instance = new ExplosionTypeEmp();
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
        if (damage == null || intensity == 0) { return; }

        if (_entityManager.TryGetComponent<EmpComponent>(entity, out var emp))
        {
            if (emp.Enabled)
            {
                var ev = new EmpEvent(intensity);
                _entityManager.EventBus.RaiseLocalEvent(entity, ref ev, false);
            }
        }
    }
}

