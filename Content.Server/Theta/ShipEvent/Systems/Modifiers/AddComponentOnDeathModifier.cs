using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Mobs;
using Content.Shared.Theta;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Prototypes;

public partial class AddComponentOnDeathModifier : ShipEventModifier, IEntityEventSubscriber
{
    [DataField("components")]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    private IEntityManager _entMan;

    public override void OnApply()
    {
        base.OnApply();
        _entMan ??= IoCManager.Resolve<IEntityManager>();
        _entMan.EventBus.SubscribeLocalEvent<ShipEventTeamMarkerComponent, MobStateChangedEvent>(OnStateChange);
    }

    private void OnStateChange(EntityUid uid, ShipEventTeamMarkerComponent marker, MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            ThetaHelpers.AddComponentsFromRegistry(uid, Components);
    }

    public override void OnRemove()
    {
        base.OnRemove();
        _entMan.EventBus.UnsubscribeEvents(this);
    }
}