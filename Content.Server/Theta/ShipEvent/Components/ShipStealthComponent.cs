namespace Content.Server.Theta.ShipEvent.Components;


[RegisterComponent]
public sealed partial class ShipStealthComponent : Component
{
    /// <summary>
    /// In seconds
    /// </summary>
    [DataField("stealthDuration", required: true), ViewVariables(VVAccess.ReadWrite)]
    public int StealthDuration;

    /// <summary>
    /// In seconds
    /// </summary>
    [DataField("stealthCooldown", required: true), ViewVariables(VVAccess.ReadWrite)]
    public int StealthCooldown;
}
