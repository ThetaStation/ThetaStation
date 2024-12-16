namespace Content.Shared.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipEventActionStorageComponent : Component
{
    public EntityUid? TeamViewActionUid;
    public EntityUid? CaptainMenuActionUid;
    public EntityUid? ReturnToLobbyActionUid;
}
