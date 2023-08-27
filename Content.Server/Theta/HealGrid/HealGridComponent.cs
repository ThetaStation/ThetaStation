namespace Content.Server.Theta.HealGrid;

[RegisterComponent]
public sealed class HealGridComponent : Component
{
    [DataField("healths")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int AvailableHealths;
}
