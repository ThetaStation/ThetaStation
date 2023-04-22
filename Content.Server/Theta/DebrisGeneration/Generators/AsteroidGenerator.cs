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
    
    public override EntityUid Generate(DebrisGenerationSystem sys, Vector2 position)
    {
        var tileDefMan = sys.TileDefMan;
        
        byte[,] tileArray = GenerateTileArray(sys.Rand);

        var gridComp = sys.MapMan.CreateGrid(sys.TargetMap);
        var gridUid = gridComp.Owner;
        var transform = sys.EntMan.GetComponent<TransformComponent>(gridUid);
        transform.Coordinates = new EntityCoordinates(transform.Coordinates.EntityId, position);

        Vector2i posRounded = (Vector2i)position.Rounded();
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (tileArray[x, y] == 1)
                {
                    Vector2i spawnPos = new Vector2i(posRounded.X + x, posRounded.Y + y);
                    gridComp.SetTile(spawnPos, new Tile(tileDefMan[FloorId].TileId));
                    if (sys.Rand.Prob(Erosion))
                        continue;
                    sys.EntMan.SpawnEntity(WallPrototypeId, new EntityCoordinates(gridUid, spawnPos));
                }
            }
        }

        return gridUid;
    }

    /// <summary>
    /// Generates array representing asteroid's shape
    /// </summary>
    /// <returns></returns>
    private byte[,] GenerateTileArray(IRobustRandom random)
    {
        byte[,] tileArray = new byte[Size + 1,Size + 1];
        Vector2 lastCirclePos = Vector2.Zero;
        int lastCircleRadius = 0;
        
        for (int n = 0; n < CircleAmount; n++)
        {
            for (int m = 0; m < 100; m++)
            {
                Vector2i pos = new Vector2i(random.Next(0 + MinCircleRadius, Size - MinCircleRadius), 
                    random.Next(0 + MinCircleRadius, Size - MinCircleRadius));

                int maxRadius = MaxCircleRadius;
                if (pos.X + maxRadius > Size)
                    maxRadius = Size - pos.X;
                if (pos.X - maxRadius < 0)
                    maxRadius = pos.X;
                if (pos.Y + maxRadius > Size)
                    maxRadius = Size - pos.Y;
                if (pos.Y - maxRadius < 0)
                    maxRadius = pos.Y;

                if ((pos - lastCirclePos).Length < lastCircleRadius + maxRadius || lastCirclePos == Vector2.Zero)
                {
                    PlaceCircle(ref tileArray, pos, maxRadius);
                    lastCirclePos = pos;
                    break;
                }
            }
        }

        return tileArray;
    }
    
    private void PlaceCircle(ref byte[,] tileArray, Vector2i pos, int radius)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                    tileArray[pos.X + x, pos.Y + y] = 1;
            }
        }
    }
}
