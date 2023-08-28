namespace Content.Server.Explosion.Components;

[RegisterComponent]
public sealed class TeleportPointComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("teleportId", required: true)]
    public string? TeleportId;
}
