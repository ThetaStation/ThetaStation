using System.Reflection;
using FastAccessors;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class MultiplyComponentFieldModifier : ShipEventModifier
{
    [DataField("component", required: true)]
    public Component Component = default!;

    [DataField("property", required: true)]
    public string Property = "";

    [DataField("multiplier", required: true)]
    public float Multiplier;

    private HashSet<IComponent> _modifiedComps = new();
    private Type? _compType;
    private MemberInfo? _compProperty;
    private IEntityManager _entMan;

    public override void OnApply()
    {
        base.OnApply();
        _entMan ??= IoCManager.Resolve<IEntityManager>();
        _compType ??= Component.GetType();
        _compProperty ??= _compType.GetMember(Property)[0];
        if (_compProperty == null || _compType == null)
        {
            Logger.Error("MultiplyComponentFieldModifier failed to resolve component's type/property");
            return;
        }

        foreach ((var _, var comp) in _entMan.GetAllComponents(_compType))
        {
            TryModifyComp(comp, Multiplier);
        }

        IoCManager.Resolve<IEntityManager>().ComponentAdded += OnCompAdd;
    }

    private void OnCompAdd(AddedComponentEventArgs args)
    {
        if (args.ComponentType.Type == _compType)
            TryModifyComp(args.BaseArgs.Component, Multiplier);
    }

    private void TryModifyComp(IComponent comp, float multiplier)
    {
        _modifiedComps.Add(comp);

        float? oldValue = (float?) ThetaHelpersServer.GetMemberValue(_compProperty!, comp);
        if (oldValue == null)
            return;

        ThetaHelpersServer.SetMemberValue(_compProperty!, comp, oldValue * multiplier);
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