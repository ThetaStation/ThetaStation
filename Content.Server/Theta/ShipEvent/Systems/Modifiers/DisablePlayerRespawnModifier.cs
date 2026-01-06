namespace Content.Server.Theta.ShipEvent.Systems.Modifiers;

public partial class DisablePlayerRespawnModifier : ShipEventModifier
{
    private ShipEventTeamSystem _shipSys;

    public override void OnApply()
    {
        base.OnApply();
        _shipSys ??= IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShipEventTeamSystem>();
        _shipSys.AllowPlayerRespawn = false;
    }

    public override void OnRemove()
    {
        base.OnRemove();
        _shipSys.AllowPlayerRespawn = true;
    }
}