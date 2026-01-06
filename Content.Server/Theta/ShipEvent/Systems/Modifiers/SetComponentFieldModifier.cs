using System.Reflection;

namespace Content.Server.Theta.ShipEvent.Systems.Modifiers;

public partial class SetComponentFieldModifier : ShipEventModifier, IEntityEventSubscriber
{
    [DataField("component", required: true)]
    public Component Component = default!;

    [DataField("property", required: true)]
    public string Property = "";

    [DataField("valueInt")]
    public int? ValueInt = null;

    [DataField("valueFloat")]
    public float? ValueFloat = null;

    [DataField("valueBool")]
    public bool? ValueBool = null;

    [DataField("valueStr")]
    public string? ValueStr = null;

    private Dictionary<IComponent, object?> _oldValues = new();
    private Type? _compType;
    private MemberInfo? _compProperty;
    private object? _value;
    private IEntityManager _entMan;

    public override void OnApply()
    {
        base.OnApply();
        _entMan ??= IoCManager.Resolve<IEntityManager>();
        _compType ??= Component.GetType();
        _compProperty ??= _compType.GetMember(Property)[0];
        if (_compProperty == null || _compType == null)
        {
            Logger.Error("SetComponentFieldModifier failed to resolve component's type/property");
            return;
        }
        //select non null value
        _value = ((object?) ValueInt ?? ValueFloat) ?? ((object?) ValueBool ?? ValueStr);

        foreach ((var _, var comp) in _entMan.GetAllComponents(_compType))
        {
            _oldValues[comp] = ThetaHelpersServer.GetMemberValue(_compProperty, comp);
            TryModifyComp(comp, _value);
        }

        _entMan.ComponentAdded += OnCompAdd;
    }

    private void OnCompAdd(AddedComponentEventArgs args)
    {
        if (args.ComponentType.Type == _compType)
            TryModifyComp(args.BaseArgs.Component, _value);
    }

    private void TryModifyComp(IComponent comp, object? value)
    {
        ThetaHelpersServer.SetMemberValue(_compProperty!, comp, value);
    }

    public override void OnRemove()
    {
        base.OnRemove();

        foreach ((var comp, var value) in _oldValues)
        {
            if (comp.Deleted)
                continue;
            TryModifyComp(comp, value);
        }

        _oldValues.Clear();
    }
}