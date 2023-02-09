using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events.Theta;

public sealed class ShipEvent : StationEventSystem
{
    [Dependency] private readonly MapLoaderSystem _map = default!;

    public static List<string> ShipGrids = new()
    {
        "/Maps/Shuttles/ship_test_1.yml",
    };

    public override string Prototype => "ShipEvent";

    public override void Started()
    {
        base.Started();

        Vector2i? lastDir = null;
        var spawnCount = 2;
        var spawned = 0;
        var failsafe = 0;
        while (++failsafe != 100)
        {
            if (spawnCount == spawned)
            {
                break;
            }

            if (!TryFindRandomTile(out _, out _, out var targetGrid, out var targetTile))
            {
                continue;
            }

            spawned++;

            var mapPos = (Vector2i) targetTile.ToMapPos(EntityManager);
            if (lastDir is null)
            {
                lastDir = RobustRandom.NextAngle().GetDir().ToIntVec();
            }
            else
            {
                lastDir = lastDir.Value * -1; // invert vector
            }

            mapPos += lastDir.Value * RobustRandom.Next(500, 750);
            var mapLoadOptions = new MapLoadOptions
            {
                Rotation = RobustRandom.NextAngle(),
                Offset = mapPos,
                LoadMap = false,
            };
            _map.Load(Transform(targetGrid).MapID, RobustRandom.Pick(ShipGrids), mapLoadOptions);
            Sawmill.Info($"Spawning the ship test shuttle at {targetTile}");
        }
    }
}
