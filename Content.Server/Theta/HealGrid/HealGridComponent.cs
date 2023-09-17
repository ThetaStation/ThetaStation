namespace Content.Server.Theta.HealGrid;

[RegisterComponent]
public sealed partial class HealGridComponent : Component
{
    [DataField("health")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int AvailableHealth;
}
