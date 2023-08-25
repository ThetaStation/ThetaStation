using Content.Shared.Dataset;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem
{
    public readonly List<MapCoordinates> PickupPositions = new();
    public readonly List<EntityUid> Pickups = new();

    public int PickupsPositionsCount;
    public string PickupsDatasetPrototype = "";
    public float PickupsSpawnInterval;
    public float PickupMinDistance;

    private TimeSpan _nextPickupsSpawn;

    private void CheckPickupsTimer()
    {
        if (_timing.CurTime < _nextPickupsSpawn)
            return;

        if(PickupPositions.Count == 0)
            FindPickupPositions();

        DeletePickups();
        SpawnPickups();

        _nextPickupsSpawn = _timing.CurTime + TimeSpan.FromSeconds(PickupsSpawnInterval);
    }

    private void FindPickupPositions()
    {
        PickupPositions.Clear();

        var areaBounds = GetPlayAreaBounds();
        areaBounds = areaBounds.Scale(0.8f);

        const short maxAttempts = 30;
        var attempts = 0;
        while (PickupPositions.Count != PickupsPositionsCount)
        {
            if(attempts == maxAttempts)
                break;

            var randomX = _random.Next((int) areaBounds.Left, (int) areaBounds.Right);
            var randomY = _random.Next((int) areaBounds.Bottom, (int) areaBounds.Top);

            var mapPos = new MapCoordinates(randomX, randomY, TargetMap);
            if(_mapMan.TryFindGridAt(mapPos, out _, out _) || !CanPlacePosition(mapPos))
            {
                attempts++;
                continue;
            }

            attempts = 0;
            PickupPositions.Add(mapPos);
        }
    }

    private bool CanPlacePosition(MapCoordinates coordinates)
    {
        foreach (var otherPos in PickupPositions)
        {
            if (coordinates.InRange(otherPos, PickupMinDistance))
                return false;
        }

        return true;
    }

    private void SpawnPickups()
    {
        foreach (var mapPos in PickupPositions)
        {
            var pickupPrototype = _random.Pick(_protMan.Index<DatasetPrototype>(PickupsDatasetPrototype));
            var entityUid = Spawn(pickupPrototype, mapPos);
            Pickups.Add(entityUid);
        }
    }

    private void DeletePickups()
    {
        foreach (var entityUid in Pickups)
        {
            QueueDel(entityUid);
        }

        Pickups.Clear();
    }
}
