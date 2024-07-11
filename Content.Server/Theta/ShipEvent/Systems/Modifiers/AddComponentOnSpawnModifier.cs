using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Theta;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Prototypes;

public partial class AddComponentOnSpawnModifier : ShipEventModifier, IEntityEventSubscriber
{
    [DataField("components")]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    private IEntityManager _entMan;

    public override void OnApply()
    {
        base.OnApply();
        _entMan ??= IoCManager.Resolve<IEntityManager>();
        _entMan.EventBus.SubscribeLocalEvent<ShipEventTeamMarkerComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnMindAdded(EntityUid uid, ShipEventTeamMarkerComponent marker, MindAddedMessage ev)
    {
        if (!_entMan.HasComponent<GhostComponent>(uid))
            ThetaHelpers.AddComponentsFromRegistry(uid, Components);
    }

    public override void OnRemove()
    {
        base.OnRemove();
        _entMan.EventBus.UnsubscribeEvents(this);
    }
}