using Content.Shared.Theta.ShipEvent;

namespace Content.Client.Theta.ShipEvent.Systems;

public delegate void LootboxInfoHandler(LootboxInfo info);

/// <summary>
/// Fetches data about lootboxes from server. The only reason why this exists is because I don't want to use netmessages in radar lootbox module
/// </summary>
public sealed class LootboxInfoSystem : EntitySystem
{
    public event LootboxInfoHandler? OnLootboxInfoReceived;
    private float syncTimer;
    private const float syncInterval = 5;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<LootboxInfo>(RaiseLootboxInfoEvent);
    }

    public override void Update(float dt)
    {
        base.Update(dt);
        syncTimer += dt;
        if (syncTimer >= syncInterval)
        {
            syncTimer = 0;
            RequestLootboxInfo();
        }
    }

    private void RaiseLootboxInfoEvent(LootboxInfo ev)
    {
        OnLootboxInfoReceived?.Invoke(ev);
    }

    public void RequestLootboxInfo()
    {
        RaiseNetworkEvent(new LootboxInfoRequest());
    }
}
