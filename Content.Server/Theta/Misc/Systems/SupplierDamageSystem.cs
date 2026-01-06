using Content.Server.Power.Components;
using Content.Server.Theta.Misc.Components;
using Content.Shared.Damage;
using Robust.Shared.Timing;

namespace Content.Server.Theta.Misc.Systems;

public sealed class SupplierDamageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageSys = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PowerSupplierComponent, DamageableComponent, SupplierDamageComponent>();
        while (query.MoveNext(out var uid, out var supplier, out var damageable, out var supdamage))
        {
            if (supdamage.NextUpdate > _timing.CurTime)
                continue;

            DamageSpecifier damage = supdamage.Damage * supplier.CurrentSupply * supdamage.DamageMultiplier;
            _damageSys.TryChangeDamage(uid, damage, damageable: damageable);
            supdamage.NextUpdate = _timing.CurTime + supdamage.UpdateInterval;
        }
    }
}
