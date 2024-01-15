namespace Content.Server.Theta.TeleportationBeacon;

[RegisterComponent]
public sealed partial class TeleportationBeaconComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("teleportId", required: true)]
    public string? TeleportId;
}
