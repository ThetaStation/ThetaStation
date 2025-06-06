using System.Threading;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Theta.ShipEvent.Systems.Modifiers;

//todo: should be splitted into several modifiers
public partial class SpaceDeathModifier : ShipEventModifier
{
    [DataField("damage", required: true)]
    public DamageSpecifier Damage;

    private CancellationTokenSource _tokenSource;
    private Timer _timer;
    private IEntityManager _entMan;
    private DamageableSystem _dmgSys;
    private ITimerManager _timerMan;

    public override void OnApply()
    {
        base.OnApply();
        _tokenSource = new();
        _entMan ??= IoCManager.Resolve<IEntityManager>();
        _dmgSys ??= _entMan.EntitySysManager.GetEntitySystem<DamageableSystem>();
        _timerMan ??= IoCManager.Resolve<ITimerManager>();
        _timer = new Timer(1000, true, OnUpdate);
        _timerMan.AddTimer(_timer, _tokenSource.Token);
    }

    public void OnUpdate()
    {
        var enumerator = _entMan.EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        //this is required since when player will die team system will try to delete his body,
        //modifying query and causing it to throw
        var enumeratorCopy = new List<(EntityUid, TransformComponent)>();
        while (enumerator.MoveNext(out var uid, out var _, out var form))
        {
            enumeratorCopy.Add((uid, form));
        }

        foreach ((var uid, var form) in enumeratorCopy)
        {
            if (form.GridUid == null)
                _dmgSys.TryChangeDamage(uid, Damage);
        }
    }

    public override void OnRemove()
    {
        base.OnRemove();
        _tokenSource.Cancel();
        _tokenSource.Dispose();
    }
}