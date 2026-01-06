using Content.Shared.Theta;
using Robust.Shared.Prototypes;

namespace Content.Server.Theta.ShipEvent.Systems.Modifiers;

public partial class AddComponentOnSpawnModifier : ShipEventModifier, IEntityEventSubscriber
{
    [DataField("components")]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();

    private ShipEventTeamSystem _shipSys;

    public override void OnApply()
    {
        base.OnApply();
        _shipSys ??= IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ShipEventTeamSystem>();
        _shipSys.OnPlayerSpawn += OnSpawn;
    }

    private void OnSpawn(EntityUid uid)
    {
        ThetaHelpers.AddComponentsFromRegistry(uid, Components);
    }

    public override void OnRemove()
    {
        base.OnRemove();
        _shipSys.OnPlayerSpawn -= OnSpawn;
    }
}