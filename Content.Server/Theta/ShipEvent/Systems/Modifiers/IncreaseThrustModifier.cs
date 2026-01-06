using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;

namespace Content.Server.Theta.ShipEvent.Systems.Modifiers;

//todo: too specific
//and no, you can't just multiply thrust for each thruster and expect for it to work
public partial class IncreaseThrustModifier : ShipEventModifier
{
    [DataField("multiplier", required: true)]
    public float Multiplier;

    private IEntityManager _entMan;
    private ThrusterSystem _thrusterSys;
    private HashSet<ShuttleComponent> _modifiedComps = new();

    public override void OnApply()
    {
        base.OnApply();
        _entMan ??= IoCManager.Resolve<IEntityManager>();
        _thrusterSys ??= _entMan.EntitySysManager.GetEntitySystem<ThrusterSystem>();

        foreach (var comp in _entMan.EntityQuery<ShuttleComponent>())
        {
            TryModifyComp(comp, Multiplier);
        }
    }

    private void TryModifyComp(ShuttleComponent comp, float multiplier)
    {
        comp.LinearThrust[0] *= multiplier;
        comp.LinearThrust[1] *= multiplier;
        comp.LinearThrust[2] *= multiplier;
        comp.LinearThrust[3] *= multiplier;
        comp.AngularThrust *= multiplier;
        comp.BaseMaxLinearVelocity *= multiplier;
    }

    public override void OnRemove()
    {
        base.OnRemove();

        foreach (var comp in _modifiedComps)
        {
            if (comp.Deleted)
                continue;
            TryModifyComp(comp, 1 / Multiplier);
        }

        _modifiedComps.Clear();
    }
}