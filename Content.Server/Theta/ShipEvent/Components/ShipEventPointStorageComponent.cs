namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipEventPointStorageComponent : Component
{
    [DataField("points")]
    public int Points;
}
