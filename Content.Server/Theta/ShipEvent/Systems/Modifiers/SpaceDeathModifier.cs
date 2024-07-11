using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Damage;

//todo: this should be more generic, but I'm out of ideas how to do so
public partial class SpaceDeathModifier : ShipEventModifier
{
    [DataField("damage", required: true)]
    public DamageSpecifier Damage;

    public override void OnApply()
    {
        base.OnApply();
    }

    public void OnUpdate()
    {
    }

    public override void OnRemove()
    {
        base.OnRemove();
    }
}