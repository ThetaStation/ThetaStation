using System.Linq;
using System.Numerics;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Theta.DebrisGeneration;

/// <summary>
/// System providing modular, procedural generation of various structures
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
    private Dictionary<Vector2i, HashSet<SectorRange>> spawnSectors = new(); //sector pos => free ranges in this sector
    private Dictionary<Vector2i, double> spawnSectorVolumes = new(); //sector pos => occupied volume in this sector

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
        Vector2i startPos,
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

            var spawnPos = GenerateSpawnPosition((Box2i)gridComp.LocalAABB.Enlarged(structProt.MinDistance));

            if (spawnPos == null)
            {
                Logger.Warning("Debris generation, GenerateDebris: Failed to find spawn position, deleting grid");
                EntityManager.DeleteEntity(grid);
                continue;
            }

            Vector2 pos = spawnPos.Value;
            pos.X += structProt.MinDistance - gridComp.LocalAABB.Left;
            pos.Y += structProt.MinDistance - gridComp.LocalAABB.Bottom;

            gridForm.Coordinates = new EntityCoordinates(gridForm.Coordinates.EntityId, pos);
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

    /// <summary>
    /// Randomly places specified structure onto map. Does not optimise collision checking in any way
    /// </summary>
    public EntityUid RandomPosSpawn(MapId targetMap, Vector2 startPos, int maxOffset, int tries,
        StructurePrototype structure, List<Processor>? extraProcessors = null, bool forceIfFailed = false)
    {
        TargetMap = targetMap;

        var grid = structure.Generator.Generate(this, TargetMap);
        var gridComp = EntMan.GetComponent<MapGridComponent>(grid);
        var gridForm = EntMan.GetComponent<TransformComponent>(grid);
        var bounds = new Box2(startPos.X,
            startPos.Y,
            startPos.X + maxOffset - gridComp.LocalAABB.Width,
            startPos.Y + maxOffset - gridComp.LocalAABB.Height);

        var finalDistance = (int)Math.Ceiling(structure.MinDistance + Math.Max(gridComp.LocalAABB.Height, gridComp.LocalAABB.Width));

        Vector2i mapPos = Vector2i.Zero;
        var result = false;
        for (int n = 0; n < tries; n++)
        {
            mapPos = (Vector2i) Rand.NextVector2Box(
                bounds.Left + gridComp.LocalAABB.Width, 
                bounds.Bottom + gridComp.LocalAABB.Height, 
                bounds.Right - gridComp.LocalAABB.Width, 
                bounds.Top - gridComp.LocalAABB.Height
                ).Rounded();
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
        }
        else if (forceIfFailed)
        {
            Logger.Info($"Debris generation, RandomPosSpawn: Failed to find spawn position for grid {grid.ToString()}," +
                        "but forceIfFailed is set to true; proceeding to force-spawn");
            mapPos = (Vector2i) Rand.NextVector2Box(bounds.Left, bounds.Bottom, bounds.Right, bounds.Top).Rounded();

            gridForm.Coordinates = new EntityCoordinates(gridForm.Coordinates.EntityId, mapPos);
        }

        if (result || forceIfFailed)
        {
            if (extraProcessors != null)
            {
                foreach (Processor extraProc in extraProcessors)
                {
                    extraProc.Process(this, targetMap, grid, false);
                }
            }

            return grid;
        }

        Logger.Error($"Debris generation, RandomPosSpawn: Failed to find spawn position, deleting grid {grid.ToString()}");
        EntityManager.DeleteEntity(grid);
        return EntityUid.Invalid;
    }

    //Set's up collision grid
    private void SetupGrid(Vector2i startPos, int maxDebrisOffset)
    {
        for (int y = 0; y < maxDebrisOffset; y += spawnSectorSize)
        {
            for (int x = 0; x < maxDebrisOffset; x += spawnSectorSize)
            {
                Vector2i sectorPos = new Vector2i(startPos.X + x, startPos.Y + y);
                spawnSectors[sectorPos] = new HashSet<SectorRange>
                {
                    new SectorRange(sectorPos.Y, sectorPos.Y + spawnSectorSize,
                        new List<(int, int)>{(sectorPos.X, sectorPos.X + spawnSectorSize)})
                };
                spawnSectorVolumes[sectorPos] = spawnSectorSize * spawnSectorSize;
            }
        }
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

    //Generates spawn position in random sector of the grid
    private Vector2? GenerateSpawnPosition(Box2i bounds)
    {
        var volume = bounds.Height * bounds.Width;
        var shuffledSectors = spawnSectors.Keys.ToList();
        Rand.Shuffle(shuffledSectors);

        foreach(var randomSector in shuffledSectors)
        {
            if (spawnSectorVolumes[randomSector] < volume)
                continue;
            var result = TryPlaceInSector(randomSector, bounds, out var spawnPos);
            if (result)
                return spawnPos;
        }

        return null;
    }

    /// <summary>
    /// Tries to find spot with enough space to fit given bounding box
    /// </summary>
    /// <param name="sectorPos">position of sector for search</param>
    /// <param name="bounds">bounding box</param>
    /// <param name="resultPos">resulting position</param>
    /// <returns></returns>
    private bool TryPlaceInSector(Vector2i sectorPos, Box2i bounds, out Vector2i resultPos)
    {
        bool result = false;
        resultPos = Vector2i.Zero;
        int lx, ly, hx, hy;
        lx = ly = hx = hy = 0;

        foreach (SectorRange range in spawnSectors[sectorPos])
        {
            foreach ((int start, int end) in range.XRanges)
            {
                if (end - start >= bounds.Width)
                {
                    if (range.Top - range.Bottom >= bounds.Height)
                    {
                        result = true;
                        lx = start;
                        hx = end - bounds.Width;
                        ly = range.Bottom;
                        hy = range.Top - bounds.Height;
                        break;
                    }
                    SectorRange combinedRange = CombineRangesVertically(spawnSectors[sectorPos], start, end, range.Bottom, range.Top, bounds.Width);

                    if (combinedRange.Top - combinedRange.Bottom >= bounds.Height)
                    {
                        result = true;
                        (int startc, int endc) = combinedRange.XRanges.First();

                        lx = startc;
                        hx = endc - bounds.Width;
                        ly = combinedRange.Bottom;
                        hy = combinedRange.Top - bounds.Height;
                        break;
                    }
                }
            }

            if (result)
            {
                resultPos = new Vector2i(Rand.Next(lx, hx), Rand.Next(ly, hy));
                break;
            }
        }

        if (result)
        {
            spawnSectors[sectorPos] = SubtractRange(spawnSectors[sectorPos],
                    RangeFromBox(
                        Box2i.FromDimensions(resultPos, new Vector2i(bounds.Width, bounds.Height))
                        )
                    );
            spawnSectorVolumes[sectorPos] -= bounds.Height * bounds.Width;
        }

        return result;
    }

    private SectorRange RangeFromBox(Box2i box)
    {
        return new SectorRange(box.Bottom, box.Top, new List<(int, int)> {(box.Left, box.Right)});
    }

    //Subtracts range from existing ranges
    private HashSet<SectorRange> SubtractRange(HashSet<SectorRange> ranges, SectorRange range)
    {
        HashSet<SectorRange> rangesNew = new();
        foreach (SectorRange rangeOther in ranges)
        {
            if (!(rangeOther.Top < range.Bottom || rangeOther.Bottom > range.Top)) //height overlap
            {
                int overlapBottom = rangeOther.Bottom;
                int overlapTop = rangeOther.Top;

                if (range.Bottom > rangeOther.Bottom)
                {
                    rangesNew.Add(new SectorRange(rangeOther.Bottom, range.Bottom, rangeOther.XRanges));
                    overlapBottom = range.Bottom;
                }
                if (range.Top < rangeOther.Top)
                {
                    rangesNew.Add(new SectorRange(range.Top, rangeOther.Top, rangeOther.XRanges));
                    overlapTop = range.Top;
                }

                rangesNew.Add(new SectorRange(overlapBottom, overlapTop, SubtractXRanges(rangeOther.XRanges, range.XRanges)));
            }
            else
            {
                rangesNew.Add(rangeOther);
            }
        }

        foreach (SectorRange nrange in rangesNew)
        {
            if (nrange.Top - nrange.Bottom == 0 || nrange.XRanges.Count == 0)
                rangesNew.Remove(nrange);
        }

        return rangesNew;
    }

    //Combines all ranges lying between endX & start X, and above/below height into a single range (with single X range)
    //with width above minWidth and combined height of included ranges
    private SectorRange CombineRangesVertically(HashSet<SectorRange> ranges, int start, int end, int heightBottom, int heightTop, int minWidth)
    {
        int startn, endn, bottomn, topn;
        (startn, endn, bottomn, topn) = (start, end, heightBottom, heightTop);

        SectorRange? GetNextFreeRange(bool above, int height)
        {
            List<SectorRange> sranges = ranges.Where(x => above ? x.Bottom >= height : x.Top <= height).
                OrderBy(x => above ? x.Top : x.Bottom).ToList();
            if (!above)
                sranges.Reverse();

            if (sranges.Count == 0)
                return null;
            SectorRange srange = sranges[0];

            if (Math.Abs((above ? srange.Bottom : srange.Top) - (above ? topn : bottomn)) > 1)
                return null;

            foreach ((int startf, int endf) in srange.XRanges)
            {
                int startnn = startf > startn ? startf : startn;
                int endnn = endf < endn ? endf : endn;
                if (endnn - startnn < minWidth)
                    continue;
                return new SectorRange(srange.Bottom, srange.Top, new List<(int, int)>{(startnn, endnn)});
            }

            return null;
        }

        while (true)
        {
            SectorRange? r = GetNextFreeRange(false, bottomn);
            if (r == null)
                break;
            bottomn = r.Value.Bottom;
            (startn, endn) = r.Value.XRanges[0];

        }

        while (true)
        {
            SectorRange? r = GetNextFreeRange(true, topn);
            if (r == null)
                break;
            topn = r.Value.Top;
            (startn, endn) = r.Value.XRanges[0];
        }

        return new SectorRange(bottomn, topn, new List<(int, int)> {(startn, endn)});
    }

    //Returns true if at least one x-range overlaps another
    private bool XRangesOverlap(List<(int, int)> ranges1, List<(int, int)> ranges2)
    {
        foreach ((int start1, int end1) in ranges1)
        {
            foreach ((int start2, int end2) in ranges2)
            {
                if (start1 < end2 && end1 > start2)
                    return true;
            }
        }

        return false;
    }

    //Returns list of inverted (occupied) ranges
    private List<(int, int)> InvertedXRanges(List<(int, int)> ranges)
    {
        List<(int, int)> results = new();
        foreach ((int start, int end) in ranges)
        {
            (int, int) nextRange = ranges.Where(r => r.Item1 > end).OrderBy(r => r.Item1).FirstOrDefault();
            if (nextRange == default)
                continue;
            results.Add((end, nextRange.Item1));
        }

        return results;
    }

    //Subtracts ranges2 from ranges1
    private List<(int, int)> SubtractXRanges(List<(int, int)> ranges1, List<(int, int)> ranges2)
    {
        List<(int, int)> SubtractOne(List<(int, int)> ranges, (int, int) range)
        {
            List<(int, int)> r = new();
            foreach ((int start, int end) in ranges)
            {
                if(!(start > range.Item2 || end < range.Item1))
                {
                    if (end > range.Item1 && range.Item1 > start)
                        r.Add((start, range.Item1));
                    if (end > range.Item2 && range.Item2 > start)
                        r.Add((range.Item2, end));
                }
                else
                {
                    r.Add((start, end));
                }
            }

            return r;
        }

        List<(int, int)> newRanges = new(ranges1);
        foreach ((int, int) range2 in ranges2)
        {
            newRanges = SubtractOne(ranges1, range2);
        }

        return newRanges;
    }

    //SectorRange represents single 'line' of sector space. It contains info about it's height (Bottom, Top) & free spaces on that height level (XRanges)
    private struct SectorRange
    {
        public int Bottom, Top;
        public List<(int, int)> XRanges;

        public SectorRange(int bottom, int top, List<(int, int)> xRanges)
        {
            Bottom = bottom;
            Top = top;
            XRanges = xRanges;
        }
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
