namespace Content.Server.Theta.Misc.Components;

/// <summary>
/// When initialized will add ambient light for map it's located on
/// </summary>
[RegisterComponent]
public sealed class AddAmbientLightComponent : Component
{
    [DataField("color", required: true), ViewVariables(VVAccess.ReadWrite)] 
    public Color AmbientLightColor;
}
