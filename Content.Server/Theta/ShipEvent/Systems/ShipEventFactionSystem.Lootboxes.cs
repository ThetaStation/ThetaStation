using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Theta.ShipEvent.UI;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem
{
    public List<(EntityUid, float)> Lootboxes = new();
    
    private void OnLootboxSpawnTriggered(EntityUid uid, ShipEventLootboxSpawnTriggerComponent trigger, UseInHandEvent args)
    {
        SpawnLootboxes(trigger.LootboxSpawnAmount);
    }
    
    private void OnPointStorageTriggered(EntityUid uid, ShipEventPointStorageComponent storage, UseInHandEvent args)
    {
        if (EntityManager.TryGetComponent<ShipEventFactionMarkerComponent>(args.User, out ShipEventFactionMarkerComponent? marker))
        {
            if (marker.Team != null)
            {
                TeamMessage(marker.Team, Loc.GetString("shipevent-pointsadded", ("points", storage.Points)));
                marker.Team.Points += storage.Points;
            }
        }
    }

    private void CheckLootboxTimer(float deltaTime)
    {
        if (_lootboxTimer > LootboxSpawnInterval)
        {
            _lootboxTimer -= LootboxSpawnInterval;
            SpawnLootboxes(LootboxSpawnAmount);
        }

        for(int i = 0; i < Lootboxes.Count; i++)
        {
            (EntityUid uid, float timer) = Lootboxes[i];
            timer -= deltaTime;
            if (timer <= 0)
            {
                EntityManager.DeleteEntity(uid);
                Lootboxes.RemoveAt(i);
                i--;
                continue;
            }

            Lootboxes[i] = (uid, timer);
        }
    }

    private void SpawnLootboxes(int amount, bool announce = true)
    {
        if(announce)
            Announce(Loc.GetString("shipevent-lootboxspawned"));
        
        for (int i = 0; i < amount; i++)
        {
            EntityUid lootbox = _debrisSys.RandomPosSpawn(TargetMap, Vector2.Zero, MaxSpawnOffset, 50, _random.Pick(LootboxPrototypes), LootboxProcessors);
            if (!lootbox.IsValid())
            {
                Logger.Warning("Ship event faction system, SpawnLootboxes: Failed to spawn lootbox! Continuing.");
                continue;
            }
            Lootboxes.Add((lootbox, LootboxLifetime));
        }
    }
}
