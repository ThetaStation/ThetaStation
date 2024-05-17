namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class HealGridComponent : Component
{
    [DataField("health")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int AvailableHealth;
}
