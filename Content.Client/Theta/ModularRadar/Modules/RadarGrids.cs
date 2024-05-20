using System.Linq;
using System.Numerics;
using Content.Client.Shuttles.UI;
using Content.Shared.Random;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarGrids : RadarModule
{
    [Dependency] private readonly IParallelManager _parallel = default!;
    private readonly SharedShuttleSystem _shuttles;
    private readonly SharedMapSystem Maps;
    private readonly SharedTransformSystem _transform;

    private GridDrawJob _drawJob;

    // Cache grid drawing data as it can be expensive to build
    public readonly Dictionary<EntityUid, GridDrawData> GridData = new();

    public bool ShowIFF { get; set; } = true;

    // Per-draw caching
    private readonly List<Vector2i> _gridTileList = new();
    private readonly HashSet<Vector2i> _gridNeighborSet = new();
    private readonly List<(Vector2 Start, Vector2 End)> _edges = new();

    private Vector2[] _allVertices = Array.Empty<Vector2>();

    private (DirectionFlag, Vector2i)[] _neighborDirections;

    private List<Entity<MapGridComponent>> _grids = new();

    public RadarGrids(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _shuttles = EntManager.System<SharedShuttleSystem>();
        _transform = EntManager.System<SharedTransformSystem>();

        Maps = EntManager.System<SharedMapSystem>();

        _drawJob = new GridDrawJob()
        {
            ScaledVertices = _allVertices,
        };

        _neighborDirections = new (DirectionFlag, Vector2i)[4];

        for (var i = 0; i < 4; i++)
        {
            var dir = (DirectionFlag) Math.Pow(2, i);
            var dirVec = dir.AsDir().ToIntVec();
            _neighborDirections[i] = (dir, dirVec);
        }
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        var fixturesQuery = EntManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = EntManager.GetEntityQuery<PhysicsComponent>();

        // Draw our grid in detail
        var ourGridId = ParentCoordinates!.Value.GetGridUid(EntManager);
        if (EntManager.TryGetComponent<MapGridComponent>(ourGridId, out var ourGrid) &&
            fixturesQuery.HasComponent(ourGridId.Value))
        {
            var ourGridMatrix = _transform.GetWorldMatrix(ourGridId.Value);
            Matrix3.Multiply(in ourGridMatrix, in parameters.DrawMatrix, out var matrix);
            var color = _shuttles.GetIFFColor(ourGridId.Value, self: true);

            DrawGrid(handle, matrix, (ourGridId.Value, ourGrid), color);
        }

        var mapPos = ParentCoordinates.Value.ToMap(EntManager, _transform);
        var rot = Radar.GetMatrixRotation();// TODO: Я ХЗ НУЖНО ЛИ ЭТО ИЛИ НЕ НУЖНО НЕ ЗНАЮ !!!ПРОВЕРИТЬ!!! + ParentRotation!.Value;
        var viewBounds = new Box2Rotated(new Box2(-WorldRange, -WorldRange, WorldRange, WorldRange).Translated(mapPos.Position), rot, mapPos.Position);
        var viewAABB = viewBounds.CalcBoundingBox();

        _grids.Clear();
        MapManager.FindGridsIntersecting(mapPos.MapId, new Box2(mapPos.Position - MaxRadarRangeVector, mapPos.Position + MaxRadarRangeVector), ref _grids, approx: true, includeMap: false);
// Draw other grids... differently
        foreach (var grid in _grids)
        {
            var gUid = grid.Owner;
            if (gUid == ourGridId || !fixturesQuery.HasComponent(gUid))
                continue;

            var gridBody = bodyQuery.GetComponent(gUid);
            EntManager.TryGetComponent<IFFComponent>(gUid, out var iff);

            if (!_shuttles.CanDraw(gUid, gridBody, iff))
                continue;

            var gridMatrix = _transform.GetWorldMatrix(gUid);
            Matrix3.Multiply(in gridMatrix, in parameters.DrawMatrix, out var matty);
            var color = _shuttles.GetIFFColor(grid, self: false, iff);

            // Others default:
            // Color.FromHex("#FFC000FF")
            // Hostile default: Color.Firebrick
            var labelName = _shuttles.GetIFFLabel(grid, self: false, iff);

            if (ShowIFF &&
                 labelName != null)
            {
                var gridBounds = grid.Comp.LocalAABB;

                var gridCentre = matty.Transform(gridBody.LocalCenter);
                gridCentre.Y = -gridCentre.Y;
                var distance = gridCentre.Length();
                var labelText = Loc.GetString("shuttle-console-iff-label", ("name", labelName),
                    ("distance", $"{distance:0.0}"));

                // yes 1.0 scale is intended here.
                var labelDimensions = handle.GetDimensions(Radar.Font, labelText, 1f);

                // y-offset the control to always render below the grid (vertically)
                var yOffset = Math.Max(gridBounds.Height, gridBounds.Width) * MinimapScale / 1.8f;

                // The actual position in the UI. We offset the matrix position to render it off by half its width
                // plus by the offset.
                var uiPosition = ScalePosition(gridCentre)- new Vector2(labelDimensions.X / 2f, -yOffset);

                // Look this is uggo so feel free to cleanup. We just need to clamp the UI position to within the viewport.
                uiPosition = new Vector2(Math.Clamp(uiPosition.X, 0f, PixelWidth - labelDimensions.X ),
                    Math.Clamp(uiPosition.Y, 0f, PixelHeight - labelDimensions.Y));

                handle.DrawString(Radar.Font, uiPosition, labelText, color);
            }

            // Detailed view
            var gridAABB = gridMatrix.TransformBox(grid.Comp.LocalAABB);

            // Skip drawing if it's out of range.
            if (!gridAABB.Intersects(viewAABB))
                continue;

            DrawGrid(handle, matty, grid, color);
        }
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, Entity<MapGridComponent> grid, Color color, float alpha = 0.01f)
    {
        var rator = Maps.GetAllTilesEnumerator(grid.Owner, grid.Comp);
        var minimapScale = MinimapScale;
        var midpoint = new Vector2(MidPoint, MidPoint);
        var tileSize = grid.Comp.TileSize;

        // Check if we even have data
        // TODO: Need to prune old grid-data if we don't draw it.
        var gridData = GridData.GetOrNew(grid.Owner);

        if (gridData.LastBuild < grid.Comp.LastTileModifiedTick)
        {
            gridData.Vertices.Clear();
            _gridTileList.Clear();
            _gridNeighborSet.Clear();

            // Okay so there's 2 steps to this
            // 1. Is that get we get a set of all tiles. This is used to decompose into triangle-strips
            // 2. Is that we get a list of all tiles. This is used for edge data to decompose into line-strips.
            while (rator.MoveNext(out var tileRef))
            {
                var index = tileRef.Value.GridIndices;
                _gridNeighborSet.Add(index);
                _gridTileList.Add(index);

                var bl = Maps.TileToVector(grid, index);
                var br = bl + new Vector2(tileSize, 0f);
                var tr = bl + new Vector2(tileSize, tileSize);
                var tl = bl + new Vector2(0f, tileSize);

                gridData.Vertices.Add(bl);
                gridData.Vertices.Add(br);
                gridData.Vertices.Add(tl);

                gridData.Vertices.Add(br);
                gridData.Vertices.Add(tl);
                gridData.Vertices.Add(tr);
            }

            gridData.EdgeIndex = gridData.Vertices.Count;
            _edges.Clear();

            foreach (var index in _gridTileList)
            {
                // We get all of the raw lines up front
                // then we decompose them into longer lines in a separate step.
                foreach (var (dir, dirVec) in _neighborDirections)
                {
                    var neighbor = index + dirVec;

                    if (_gridNeighborSet.Contains(neighbor))
                        continue;

                    var bl = Maps.TileToVector(grid, index);
                    var br = bl + new Vector2(tileSize, 0f);
                    var tr = bl + new Vector2(tileSize, tileSize);
                    var tl = bl + new Vector2(0f, tileSize);

                    // Could probably rotate this but this might be faster?
                    Vector2 actualStart;
                    Vector2 actualEnd;

                    switch (dir)
                    {
                        case DirectionFlag.South:
                            actualStart = bl;
                            actualEnd = br;
                            break;
                        case DirectionFlag.East:
                            actualStart = br;
                            actualEnd = tr;
                            break;
                        case DirectionFlag.North:
                            actualStart = tr;
                            actualEnd = tl;
                            break;
                        case DirectionFlag.West:
                            actualStart = tl;
                            actualEnd = bl;
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    _edges.Add((actualStart, actualEnd));
                }
            }

            // Decompose the edges into longer lines to save data.
            // Now we decompose the lines into longer lines (less data to send to the GPU)
            var decomposed = true;

            while (decomposed)
            {
                decomposed = false;

                for (var i = 0; i < _edges.Count; i++)
                {
                    var (start, end) = _edges[i];
                    var neighborFound = false;
                    var neighborIndex = 0;
                    Vector2 neighborStart;
                    Vector2 neighborEnd = Vector2.Zero;

                    // Does our end correspond with another start?
                    for (var j = i + 1; j < _edges.Count; j++)
                    {
                        (neighborStart, neighborEnd) = _edges[j];

                        if (!end.Equals(neighborStart))
                            continue;

                        neighborFound = true;
                        neighborIndex = j;
                        break;
                    }

                    if (!neighborFound)
                        continue;

                    // Check if our start and the neighbor's end are collinear
                    if (!CollinearSimplifier.IsCollinear(start, end, neighborEnd, 10f * float.Epsilon))
                        continue;

                    decomposed = true;
                    _edges[i] = (start, neighborEnd);
                    _edges.RemoveAt(neighborIndex);
                }
            }

            gridData.Vertices.EnsureCapacity(_edges.Count * 2);

            foreach (var edge in _edges)
            {
                gridData.Vertices.Add(edge.Start);
                gridData.Vertices.Add(edge.End);
            }

            gridData.LastBuild = grid.Comp.LastTileModifiedTick;
        }

        var totalData = gridData.Vertices.Count;
        var triCount = gridData.EdgeIndex;
        var edgeCount = totalData - gridData.EdgeIndex;
        Extensions.EnsureLength(ref _allVertices, totalData);

        _drawJob.MidPoint = midpoint;
        _drawJob.Matrix = matrix;
        _drawJob.MinimapScale = minimapScale;
        _drawJob.Vertices = gridData.Vertices;
        _drawJob.ScaledVertices = _allVertices;

        _parallel.ProcessNow(_drawJob, totalData);

        const float BatchSize = 3f * 4096;

        for (var i = 0; i < Math.Ceiling(triCount / BatchSize); i++)
        {
            var start = (int) (i * BatchSize);
            var end = (int) Math.Min(triCount, start + BatchSize);
            var count = end - start;
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, new Span<Vector2>(_allVertices, start, count), color.WithAlpha(alpha));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, new Span<Vector2>(_allVertices, gridData.EdgeIndex, edgeCount), color);
    }


    private record struct GridDrawJob : IParallelRobustJob
    {
        public int BatchSize => 16;

        public float MinimapScale;
        public Vector2 MidPoint;
        public Matrix3 Matrix;

        public List<Vector2> Vertices;
        public Vector2[] ScaledVertices;

        public void Execute(int index)
        {
            var vert = Vertices[index];
            var adjustedVert = Matrix.Transform(vert);
            adjustedVert = adjustedVert with { Y = -adjustedVert.Y };

            var scaledVert = ScalePosition(adjustedVert, MinimapScale, MidPoint);
            ScaledVertices[index] = scaledVert;
        }
    }
}
