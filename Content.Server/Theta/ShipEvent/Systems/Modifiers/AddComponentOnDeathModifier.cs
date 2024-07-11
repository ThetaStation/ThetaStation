using Content.Shared.Mobs;
using Content.Shared.Theta;
using Robust.Shared.Prototypes;

namespace Content.Server.Theta.ShipEvent.Systems.Modifiers;

public partial class AddComponentOnStateChangeModifier : ShipEventModifier, IEntityEventSubscriber
{
    [DataField("components")]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    [DataField("state")]
    public MobState State;

    private ShipEventTeamSystem _shipSys;

    public override void OnApply()
    {
        base.OnApply();
        _shipSys ??= IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShipEventTeamSystem>();
        _shipSys.OnPlayerStateChange += OnStateChange;
    }

    private void OnStateChange(EntityUid uid, MobState state)
    {
        if ((state & State) != 0)
            ThetaHelpers.AddComponentsFromRegistry(uid, Components);
    }

    public override void OnRemove()
    {
        base.OnRemove();
        _shipSys.OnPlayerStateChange -= OnStateChange;
    }
}