namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed class ShipEventPointStorageComponent : Component
{
    [DataField("points")]
    public int Points;
}
