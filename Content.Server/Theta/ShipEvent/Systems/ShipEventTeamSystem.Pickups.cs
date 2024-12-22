using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventTeamSystem
{
    public readonly List<MapCoordinates> PickupPositions = new();
    public readonly List<EntityUid> Pickups = new();

    public int PickupPositionsCount;
    public string PickupPrototype = "";
    public float PickupSpawnInterval;
    public float PickupMinDistance;

    private void InitializePickups()
    {
        FindPickupPositions();
        SpawnPickups();
    }

    private void PickupSpawn()
    {
        DeletePickups();
        SpawnPickups();
    }

    private void FindPickupPositions()
    {
        PickupPositions.Clear();

        var areaBounds = PlayArea.Scale(0.8f);

        const short maxAttempts = 30;
        var attempts = 0;
        while (PickupPositions.Count != PickupPositionsCount)
        {
            if (attempts == maxAttempts)
                break;

            var randomX = _random.Next((int) areaBounds.Left, (int) areaBounds.Right);
            var randomY = _random.Next((int) areaBounds.Bottom, (int) areaBounds.Top);

            var mapPos = new MapCoordinates(randomX, randomY, TargetMap);
            if (!CanPlacePickupPosition(mapPos))
            {
                attempts++;
                continue;
            }

            attempts = 0;
            PickupPositions.Add(mapPos);
        }
    }

    private bool CanPlacePickupPosition(MapCoordinates coordinates)
    {
        if (_mapMan.TryFindGridAt(coordinates, out _, out _))
            return false;

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
            var weight = _protMan.Index<WeightedRandomEntityPrototype>(PickupPrototype);
            var entityUid = Spawn(weight.Pick(_random), mapPos);
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
