using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Server.Theta.HealGrid;

public sealed class HealGridSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HealGridComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, HealGridComponent healComponent, TriggerEvent args)
    {
        if(args.User == null || !TryComp<MapGridComponent>(args.User, out var grid))
            return;
        var list = GetDamageableOnGrid(args.User.Value);
        _random.Shuffle(list);
        foreach (var (entityOnGrid, damageable) in list)
        {
            if(healComponent.AvailableHealths == 0)
                break;
            var heal = new DamageSpecifier(damageable.Damage);
            foreach (var (group, damage) in heal.DamageDict)
            {
                if(healComponent.AvailableHealths == 0)
                    break;

                var healingValue = healComponent.AvailableHealths - damage > 0
                    ? damage
                    : healComponent.AvailableHealths;
                heal.DamageDict[group] = healingValue;
                healComponent.AvailableHealths -= healingValue.Int();
            }

            heal = -heal;
            _damageableSystem.TryChangeDamage(entityOnGrid, heal, true, false, damageable);
        }
    }

    private List<(EntityUid, DamageableComponent)> GetDamageableOnGrid(EntityUid gridUid)
    {
        var entityUids = new List<(EntityUid, DamageableComponent)>();
        var query = EntityManager.EntityQueryEnumerator<TransformComponent, DamageableComponent>();
        while(query.MoveNext(out var uid, out var transform, out var damageable))
        {
            if(transform.GridUid != gridUid)
                continue;
            entityUids.Add((uid, damageable));
        }

        return entityUids;
    }
}
