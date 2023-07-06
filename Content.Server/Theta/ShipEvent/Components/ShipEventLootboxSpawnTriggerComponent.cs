namespace Content.Server.Theta.ShipEvent.Components;

//used in lootbox loot
[RegisterComponent]
public sealed class ShipEventLootboxSpawnTriggerComponent : Component
{
    [DataField("toSpawn")]
    public int LootboxSpawnAmount = 1;
}
