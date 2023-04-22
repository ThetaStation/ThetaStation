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


        for (int n = 0; n < debrisAmount; n++)
        {
            var structProt = PickStructure(structures);
            if (structProt == null)
            {
                Logger.Warning("Could not pick structure prototype (debris generation system)");
                continue;
            }

            Vector2 pos = startPos;
            for (int m = 0; m < 100; m++)
            {
                pos += Rand.NextVector2(maxDebrisOffset);

                if (!MapMan.FindGridsIntersecting(TargetMap, 
                        new Box2(pos - structProt.MinDistance, pos + structProt.MinDistance)).Any())
                    break;
            }

            var grid = structProt.Generator.Generate(this, pos);
            SpawnedGrids.Add(grid);
            foreach (var proc in structProt.Processors)
            {
                proc.Process(this, grid, false);
            }
        }

        foreach (var proc in globalProcessors)
        {
            proc.Process(this, MapMan.GetMapEntityId(TargetMap), true);
        }
        
        TargetMap = MapId.Nullspace;
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
}

/// <summary>
/// Generator is a base class for generating debris
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class Generator
{
    public abstract EntityUid Generate(DebrisGenerationSystem sys, Vector2 position);
}

/// <summary>
/// Processor is a base class for post-processing debris
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class Processor
{
    public abstract void Process(DebrisGenerationSystem sys, EntityUid gridUid, bool isGlobal);
}
