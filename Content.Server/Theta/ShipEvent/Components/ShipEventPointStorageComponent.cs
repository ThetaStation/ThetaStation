namespace Content.Server.Theta.ShipEvent.Components;

//used in lootbox loot
[RegisterComponent]
public sealed class ShipEventPointStorageComponent : Component
{
    [DataField("points")]
    public int Points;
}
