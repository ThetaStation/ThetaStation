using System.Linq;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Theta.DebrisGeneration;


public sealed class DebrisGenerationSystem : EntitySystem
{
    //public fields are for the systems most commonly used by generators & processors,
    //to prevent wasting a bit of time calling IoC every time & for convenience
    [Dependency] public readonly IMapManager MapMan = default!;
    [Dependency] public readonly MapLoaderSystem MapLoader = default!;
    [Dependency] public readonly ITileDefinitionManager TileDefMan = default!;
    [Dependency] public readonly IPrototypeManager ProtMan = default!;
    [Dependency] public readonly IRobustRandom Rand = default!;
    public IEntityManager EntMan => EntityManager;
    
    public MapId TargetMap = MapId.Nullspace;
    public List<EntityUid> SpawnedGrids = new();

    //primitive quad tree (aka plain grid) for optimising collision checks
    private const int spawnSectorSize = 100;
    private const int spawnTries = 5;
    private Dictionary<Vector2, List<(Vector2, int)>> spawnSectors = new(); //starting pos => spawn positions in this sector
    private Dictionary<Vector2, float> spawnSectorVolumes = new(); //starting pos => occupied volume in this sector

    public void GenerateDebris(
        MapId targetMap,
        Vector2 startPos,
        List<StructurePrototype> structures, 
        List<Processor> globalProcessors,
        int debrisAmount,
        int maxDebrisOffset)
    {
        if (targetMap == MapId.Nullspace || !MapMan.MapExists(targetMap))
            return;
        TargetMap = targetMap;
        MapMan.SetMapPaused(TargetMap, true);
        
        SetupGrid(startPos, maxDebrisOffset);
        
        for (int n = 0; n < debrisAmount; n++)
        {
            var structProt = PickStructure(structures);
            if (structProt == null)
            {
                Logger.Warning("Could not pick structure prototype (debris generation system)");
                continue;
            }

            var spawnPos = GenerateSpawnPosition(structProt.MinDistance);

            var grid = structProt.Generator.Generate(this, TargetMap, spawnPos);
            SpawnedGrids.Add(grid);
            foreach (var proc in structProt.Processors)
            {
                proc.Process(this, TargetMap, grid, false);
            }
        }

        foreach (var proc in globalProcessors)
        {
            proc.Process(this, TargetMap, MapMan.GetMapEntityId(TargetMap), true);
        }
        
        MapMan.SetMapPaused(TargetMap, false);
        TargetMap = MapId.Nullspace;
        Logger.Info($"Debris generation: Spawned {SpawnedGrids.Count} grids.");
        SpawnedGrids.Clear();

        spawnSectors.Clear();
        spawnSectorVolumes.Clear();
    }
    
    // Randomly picks structure from structure list, accounting for their weight
    // todo: maybe it's worth to PR this to RT (weighted random selection)
    private StructurePrototype? PickStructure(List<StructurePrototype> structures)
    {
        float totalWeight = structures.Select(s => s.SpawnWeight).Sum();
        float randFloat = Rand.NextFloat(0, totalWeight);

        StructurePrototype? picked = null;
        foreach(var structProt in structures)
        {
            if (structProt.SpawnWeight > randFloat)
            {
                picked = structProt;
                break;
            }

            randFloat -= structProt.SpawnWeight;
        }

        return picked;
    }

    private void SetupGrid(Vector2 startPos, int maxDebrisOffset)
    {
        for (int y = 0; y < maxDebrisOffset; y += spawnSectorSize)
        {
            for (int x = 0; x < maxDebrisOffset; x += spawnSectorSize)
            {
                Vector2 sectorPos = new Vector2(startPos.X + x, startPos.Y + y);
                spawnSectors[sectorPos] = new List<(Vector2, int)>();
                spawnSectorVolumes[sectorPos] = 0;
            }
        }
    }

    private Vector2 GenerateSpawnPosition(int minDistance)
    {
        var volume = Math.Pow(minDistance, 2) * 2 * Math.PI;
        var shuffledSectors = spawnSectors.Keys.ToList();
        Rand.Shuffle(shuffledSectors);
        
        foreach(var randomSector in shuffledSectors)
        {
            if (spawnSectorSize * spawnSectorSize - spawnSectorVolumes[randomSector] < volume)
                continue;
            var result = TryPlaceInSector(randomSector, minDistance, spawnTries, out var spawnPos);
            if (result)
                return spawnPos;
        }

        return Vector2.Zero;
    }

    private bool TryPlaceInSector(Vector2 sectorPos, int radius, int tries, out Vector2 pos)
    {
        pos = Vector2.Zero;
        bool result = true;
        Vector2 sectorEnd = sectorPos + spawnSectorSize;
        
        for (int t = 0; t < tries; t++)
        {
            pos = Rand.NextVector2Box(sectorPos.X, sectorPos.Y, sectorEnd.X, sectorEnd.Y);
            foreach ((Vector2 otherPos, int otherRadius) in spawnSectors[sectorPos])
            {
                if ((pos - otherPos).Length < radius + otherRadius)
                {
                    result = false;
                    break;
                }
            }

            if (!result)
            {
                result = true;
                continue;
            }
            break;
        }

        if (result)
        {
            spawnSectors[sectorPos].Add((pos, radius));
            spawnSectorVolumes[sectorPos] += (float)(Math.Pow(radius, 2) * 2 * Math.PI);
        }
        return result;
    }
}

/// <summary>
/// Generator is a base class for generating debris
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class Generator
{
    public abstract EntityUid Generate(DebrisGenerationSystem sys, MapId targetMap, Vector2 position);
}

/// <summary>
/// Processor is a base class for post-processing debris
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class Processor
{
    public abstract void Process(DebrisGenerationSystem sys, MapId targetMap, EntityUid gridUid, bool isGlobal);
}
