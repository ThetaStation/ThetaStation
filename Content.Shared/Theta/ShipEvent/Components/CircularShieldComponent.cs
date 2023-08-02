using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Components;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CircularShieldComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundConsole;
    
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Powered;

    [DataField("consumptionPerM2"), ViewVariables(VVAccess.ReadWrite)]
    public float ConsumptionPerSquareMeter;

    //specified in degrees, for prototypes
    [DataField("maxWidth"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxWidth = 360;
    
    [DataField("maxRadius"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxRadius;

    [AutoNetworkedField, DataField("color"), ViewVariables(VVAccess.ReadWrite)]
    public Color Color;

    //(datafields are for map serialization, so it's possible for mappers to create shield presets)
    [AutoNetworkedField, DataField("angle"), ViewVariables(VVAccess.ReadWrite)]
    public Angle Angle;

    [AutoNetworkedField, DataField("width"), ViewVariables(VVAccess.ReadWrite)]
    public Angle Width;

    [AutoNetworkedField, DataField("radius"), ViewVariables(VVAccess.ReadWrite)]
    public int Radius;

    [DataField("effects", serverOnly:true)]
    public List<CircularShieldEffect> Effects = new();

    public bool CanWork => Enabled && Powered;
    
    public int DesiredDraw => Enabled ? (int)(Radius * Radius * Width * 0.5 * ConsumptionPerSquareMeter) : 0;
}

[ImplicitDataDefinitionForInheritors]
public abstract class CircularShieldEffect
{
    public abstract void OnShieldInit(EntityUid uid, CircularShieldComponent shield);
    
    public abstract void OnShieldEnter(EntityUid uid, CircularShieldComponent shield);
    
    public abstract void OnShieldExit(EntityUid uid, CircularShieldComponent shield);
}

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class CircularShieldConsoleComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundShield;
}

[Serializable, NetSerializable]
public sealed class CircularShieldToggleMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class CircularShieldChangeParametersMessage : BoundUserInterfaceMessage
{
    public Angle Angle;

    public Angle Width;

    public int Radius;

    public CircularShieldChangeParametersMessage(Angle angle, Angle width, int radius)
    {
        Angle = angle;
        Width = width;
        Radius = radius;
    }
}
