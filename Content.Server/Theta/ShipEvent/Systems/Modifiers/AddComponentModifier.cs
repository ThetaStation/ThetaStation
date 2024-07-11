using Content.Shared.Theta;
using Robust.Shared.Prototypes;

namespace Content.Server.Theta.ShipEvent.Systems.Modifiers;

public partial class AddComponentModifier : ShipEventModifier
{
    [DataField("targetComponent", required: true)]
    public Component TargetComponent = default!;

    [DataField("components", required: true)]
    public ComponentRegistry Components = default!;

    private HashSet<EntityUid> _modifiedUids = new();
    private Type? _compType;
    private IEntityManager _entMan;
    private IComponentFactory _factory;

    public override void OnApply()
    {
        base.OnApply();
        _compType ??= TargetComponent.GetType();
        _entMan ??= IoCManager.Resolve<IEntityManager>();

        if (_compType == null)
        {
            Logger.Error("AddComponentModifier failed to resolve component type");
            return;
        }

        foreach ((var _, var comp) in _entMan.GetAllComponents(_compType))
        {
            AddComponents(comp.Owner);
        }

        IoCManager.Resolve<IEntityManager>().ComponentAdded += OnCompAdd;
    }

    private void OnCompAdd(AddedComponentEventArgs args)
    {
        if (args.ComponentType.Type == _compType)
            AddComponents(args.BaseArgs.Owner);
    }

    private void AddComponents(EntityUid uid)
    {
        _modifiedUids.Add(uid);
        ThetaHelpers.AddComponentsFromRegistry(uid, Components);
    }

    public override void OnRemove()
    {
        base.OnRemove();

        foreach (var uid in _modifiedUids)
        {
            if (!_entMan.EntityExists(uid))
                continue;
            ThetaHelpers.RemoveComponentsFromRegistry(uid, Components);
        }

        _modifiedUids.Clear();
    }
}