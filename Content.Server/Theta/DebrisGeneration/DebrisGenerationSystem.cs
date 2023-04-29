using System.Linq;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Theta.DebrisGeneration;

/// <summary>
/// System providing modular, procedural generation of various stru
/// </summary>
public sealed class DebrisGenerationSystem : EntitySystem
{
    //public fields are for the systems most commonly used by generators & processors,
    //to prevent wasting a bit of time calling IoC every time & for convenience
    [Dependency] public readonly IMapManager MapMan = default!;
    [Dependency] public readonly MapLoaderSystem MapLoader = default!;
    [Dependency] public readonly ITileDefinitionManager TileDefMan = default!;
    [Dependency] public readonly TransformSystem FormSys = default!;
    [Dependency] public readonly IPrototypeManager ProtMan = default!;
    [Dependency] public readonly IRobustRandom Rand = default!;
    public IEntityManager EntMan => EntityManager;
    
    public MapId TargetMap = MapId.Nullspace;
    public List<EntityUid> SpawnedGrids = new();

    //primitive quad tree (aka plain grid) for optimising collision checks
    private const int spawnSectorSize = 100;
    private const int spawnTries = 5;
    private Dictionary<Vector2, List<(Vector2, int)>> spawnSectors = new(); //starting pos => spawn positions in this sector
    private Dictionary<Vector2, double> spawnSectorVolumes = new(); //starting pos => occupied volume in this sector

    /// <summary>
    /// Randomly places specified structures onto map
    /// </summary>
    /// <param name="targetMap">selected map</param>
    /// <param name="startPos">starting position from which to spawn structures</param>
    /// <param name="structures">list of structure prototypes to spawn</param>
    /// <param name="globalProcessors">list of processors which should run after all structures were spawned</param>
    /// <param name="debrisAmount">amount of structures to spawn</param>
    /// <param name="maxDebrisOffset">max offset from startPos. startPos is being the left-lower corner of the square in which spawning positions are chosen</param>
    public void SpawnStructures(
        MapId targetMap,
        Vector2 startPos,
        int structureAmount,
        int maxOffset,
        List<StructurePrototype> structures, 
        List<Processor> globalProcessors)
    {
        if (targetMap == MapId.Nullspace || !MapMan.MapExists(targetMap))
            return;
        TargetMap = targetMap;
        MapMan.SetMapPaused(TargetMap, true);
        
        SetupGrid(startPos, maxOffset);
        
        for (int n = 0; n < structureAmount; n++)
        {
            var structProt = PickStructure(structures);
            if (structProt == null)
            {
                Logger.Warning("Debris generation, GenerateDebris: Could not pick structure prototype, skipping");
                continue;
            }

            var grid = structProt.Generator.Generate(this, TargetMap);
            var gridComp = EntMan.GetComponent<MapGridComponent>(grid);
            var gridForm = EntMan.GetComponent<TransformComponent>(grid);

            var finalDistance = (int)Math.Ceiling(structProt.MinDistance + Math.Max(gridComp.LocalAABB.Height, gridComp.LocalAABB.Width));
            var spawnPos = GenerateSpawnPosition(finalDistance);
            if (spawnPos == null)
            {
                Logger.Error("Debris generation, GenerateDebris: Failed to find spawn position, deleting grid");
                EntityManager.DeleteEntity(grid);
                continue;
            }
            
            gridForm.Coordinates = new EntityCoordinates(gridForm.Coordinates.EntityId, spawnPos.Value);
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
        Logger.Info($"Debris generation, GenerateDebris: Spawned {SpawnedGrids.Count} grids");
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

    //Set's up collision grid
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

    //Generates spawn position in random sector of the grid
    private Vector2? GenerateSpawnPosition(int distance)
    {
        var volume = distance * distance * Math.PI;
        var shuffledSectors = spawnSectors.Keys.ToList();
        Rand.Shuffle(shuffledSectors);
        
        foreach(var randomSector in shuffledSectors)
        {
            if (spawnSectorSize * spawnSectorSize - spawnSectorVolumes[randomSector] < volume)
                continue;
            var result = TryPlaceInSector(randomSector, distance, spawnTries, out var spawnPos);
            if (result)
                return spawnPos;
        }
        
        return null;
    }

    //Tries to find good position in specified sector & if it's successful, updates sector contents
    private bool TryPlaceInSector(Vector2 sectorPos, int radius, int tries, out Vector2 resultPos)
    {
        bool CanPlace(Vector2 pos)
        {
            foreach ((Vector2 otherPos, int otherRadius) in spawnSectors[sectorPos])
            {
                if ((pos - otherPos).Length < radius + otherRadius)
                {
                    return false;
                }
            }

            return true;
        }
        
        resultPos = Vector2.Zero;
        bool result = true;
        Vector2 sectorEnd = sectorPos + spawnSectorSize;
        
        for (int t = 0; t < tries; t++)
        {
            resultPos = Rand.NextVector2Box(sectorPos.X, sectorPos.Y, sectorEnd.X, sectorEnd.Y);

            if (CanPlace(resultPos))
            {
                result = true;
                break;
            }
        }

        if (result)
        {
            spawnSectors[sectorPos].Add((resultPos, radius));
            spawnSectorVolumes[sectorPos] += radius * radius * Math.PI;
        }
        return result;
    }

    /// <summary>
    /// Randomly places specified structure onto map. Does not optimise collision checking in any way
    /// </summary>
    public EntityUid RandomPosSpawn(MapId targetMap, Vector2 startPos, int maxOffset, int tries, StructurePrototype structure, List<Processor> extraProcessors)
    {
        TargetMap = targetMap;
        
        var grid = structure.Generator.Generate(this, TargetMap);
        var gridComp = EntMan.GetComponent<MapGridComponent>(grid);
        var gridForm = EntMan.GetComponent<TransformComponent>(grid);

        var finalDistance = (int)Math.Ceiling(structure.MinDistance + Math.Max(gridComp.LocalAABB.Height, gridComp.LocalAABB.Width));

        Vector2i mapPos = Vector2i.Zero;
        var result = false;
        for (int n = 0; n < tries; n++)
        {
            mapPos = (Vector2i) Rand.NextVector2Box(
                startPos.X, 
                startPos.Y, 
                startPos.X + maxOffset, 
                startPos.Y + maxOffset).Rounded();
            if (!MapMan.FindGridsIntersecting(targetMap,
                    new Box2(mapPos - finalDistance, mapPos + finalDistance)).Any())
            {
                result = true;
                break;
            }
        }
        
        TargetMap = MapId.Nullspace;
        
        if (result)
        {
            Logger.Info($"Debris generation, RandomPosSpawn: Spawned grid {grid.ToString()} successfully");
            gridForm.Coordinates = new EntityCoordinates(gridForm.Coordinates.EntityId, mapPos);
            return grid;
        }
        
        Logger.Error($"Debris generation, RandomPosSpawn: Failed to find spawn position, deleting grid {grid.ToString()}");
        EntityManager.DeleteEntity(grid);
        return EntityUid.Invalid;
    }
}

/// <summary>
/// Generator is a base class for generating debris
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class Generator
{
    public abstract EntityUid Generate(DebrisGenerationSystem sys, MapId targetMap);
}

/// <summary>
/// Processor is a base class for post-processing debris
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class Processor
{
    public abstract void Process(DebrisGenerationSystem sys, MapId targetMap, EntityUid gridUid, bool isGlobal);
}
