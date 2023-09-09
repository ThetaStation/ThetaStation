using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;


namespace Content.Server.Theta.DebrisGeneration.Generators;

/// <summary>
/// Procedural asteroid generator. Resulting shape is basically just a bunch of circles slapped onto each other
/// </summary>
public sealed class AsteroidGenerator : Generator
{
    [DataField("size", required: true)]
    public int Size;

    [DataField("circleAmount", required: true)]
    public int CircleAmount;

    [DataField("maxCircleRadius", required: true)]
    public int MaxCircleRadius;

    [DataField("minCircleRadius", required: true)]
    public int MinCircleRadius;

    /// <summary>
    /// Coefficient between 0 and 1, signifying how much walls we want to delete, to leave cavities inside generated shape
    /// </summary>
    [DataField("erosion")]
    public float Erosion;

    [DataField("floorId", required: true)]
    public string FloorId = "";

    [DataField("wallPrototypeId", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WallPrototypeId = "";

    public override EntityUid Generate(DebrisGenerationSystem sys, MapId targetMap)
    {
        var tileDefMan = sys.TileDefMan;

        var tileSet = GenerateTileSet(sys.Rand);

        var gridComp = sys.MapMan.CreateGrid(targetMap);
        var gridUid = gridComp.Owner;

        List<(Vector2i, Tile)> tiles = new();
        List<(EntityCoordinates, string)> ents = new();

        foreach (var pos in tileSet)
        {
            tiles.Add((pos, new Tile(tileDefMan[FloorId].TileId)));
            if (sys.Rand.Prob(Erosion))
                continue;
            ents.Add((new EntityCoordinates(gridUid, pos), WallPrototypeId));
        }

        gridComp.SetTiles(tiles);
        foreach ((EntityCoordinates coords, string protId) in ents)
        {
            var ent = sys.EntMan.SpawnEntity(protId, coords);
            sys.FormSys.AttachToGridOrMap(ent);
        }

        return gridUid;
    }

    private HashSet<Vector2i> GenerateTileSet(IRobustRandom random)
    {
        HashSet<Vector2i> tileSet = new();
        Vector2 lastCirclePos = Vector2.Zero;
        int lastCircleRadius = 0;

        for (int n = 0; n < CircleAmount; n++)
        {
            for (int m = 0; m < 100; m++)
            {
                if (Size - MinCircleRadius <= MinCircleRadius)
                {
                    Logger.Warning("AsteroidGenerator: size of asteroid is too small, can't place minimum radius circle properly. Please check your prototypes.");
                    return new HashSet<Vector2i>();
                }

                Vector2i pos = new Vector2i(random.Next(MaxCircleRadius, Size - MaxCircleRadius), random.Next(MaxCircleRadius, Size - MaxCircleRadius));

                int maxRadius = MaxCircleRadius;
                if (pos.X + maxRadius > Size)
                    maxRadius = Size - pos.X;
                if (pos.X - maxRadius < 0)
                    maxRadius = pos.X;
                if (pos.Y + maxRadius > Size)
                    maxRadius = Size - pos.Y;
                if (pos.Y - maxRadius < 0)
                    maxRadius = pos.Y;

                if ((pos - lastCirclePos).Length() < lastCircleRadius + maxRadius || lastCirclePos == Vector2.Zero)
                {
                    PlaceCircle(ref tileSet, pos, maxRadius);
                    lastCirclePos = pos;
                    break;
                }
            }
        }

        return tileSet;
    }

    private void PlaceCircle(ref HashSet<Vector2i> tileSet, Vector2i pos, int radius)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                    tileSet.Add(new Vector2i(x + pos.X, y + pos.Y));
            }
        }
    }
}
