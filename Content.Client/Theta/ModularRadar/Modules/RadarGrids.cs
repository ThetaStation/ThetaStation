using System.Linq;
using System.Numerics;
using Content.Shared.Random;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarGrids : RadarModule
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private readonly MapSystem _mapSystem;
    private readonly RulesSystem _ruleSystem;
    private readonly TransformSystem _formSystem;

    /// <summary>
    /// Shows a label on each radar object.
    /// </summary>
    private Dictionary<EntityUid, Control> _iffControls = new();

    private Dictionary<MapGridComponent, GridInfo> _cachedGridInfo = new();

    public bool ShowIFF { get; set; } = true;

    private float _updateEdgeTimer = 0;

    public RadarGrids(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _mapSystem = EntManager.System<MapSystem>();
        _ruleSystem = EntManager.System<RulesSystem>();
        _formSystem = EntManager.System<TransformSystem>();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (Radar.Visible)
        {
            _updateEdgeTimer += args.DeltaSeconds;
            UpdateGridEdges();
        }
    }

    private void UpdateGridEdges()
    {
        if (ParentCoordinates == null)
            return;

        var fixturesQuery = EntManager.GetEntityQuery<FixturesComponent>();
        var mapPosition = ParentCoordinates.Value.ToMap(EntManager, _formSystem);
        var ourGridId = ParentCoordinates!.Value.GetGridUid(EntManager);

        if (ourGridId == null)
            return;

        var findGridsIntersecting = MapManager.FindGridsIntersecting(mapPosition.MapId,
            new Box2(mapPosition.Position - MaxRadarRangeVector, mapPosition.Position + MaxRadarRangeVector)).ToList();
        findGridsIntersecting.Add(EntManager.GetComponent<MapGridComponent>(ourGridId.Value));

        foreach (var grid in findGridsIntersecting)
        {
            if (!fixturesQuery.HasComponent(grid.Owner))
                continue;

            if (!_cachedGridInfo.TryGetValue(grid, out var info))
            {
                var newInfo = new GridInfo(_updateEdgeTimer + GetCacheTimeExpiration(grid.Owner), GetGridEdges(grid));
                _cachedGridInfo.Add(grid, newInfo);
                continue;
            }

            if (_updateEdgeTimer > info.NextUpdateTime)
            {
                info.Edges = GetGridEdges(grid);
                info.NextUpdateTime = _updateEdgeTimer + GetCacheTimeExpiration(grid.Owner);
            }
        }
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        var fixturesQuery = EntManager.GetEntityQuery<FixturesComponent>();
        var xformQuery = EntManager.GetEntityQuery<TransformComponent>();
        var bodyQuery = EntManager.GetEntityQuery<PhysicsComponent>();
        var metaQuery = EntManager.GetEntityQuery<MetaDataComponent>();

        // Draw our grid in detail
        var ourGridId = ParentCoordinates!.Value.GetGridUid(EntManager);
        if (EntManager.TryGetComponent<MapGridComponent>(ourGridId, out var ourGrid))
        {
            var ourGridMatrix = _formSystem.GetWorldMatrix(ourGridId.Value);
            Matrix3.Multiply(in ourGridMatrix, in parameters.DrawMatrix, out var matrix);
            DrawGrid(handle, matrix, ourGrid, Color.MediumSpringGreen);
        }

        var mapPosition = ParentCoordinates.Value.ToMap(EntManager, _formSystem);

        var shown = new HashSet<EntityUid>();
        // Draw other grids... differently
        foreach (var grid in MapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - MaxRadarRangeVector, mapPosition.Position + MaxRadarRangeVector)))
        {
            if (grid.Owner == ourGridId || !fixturesQuery.HasComponent(grid.Owner))
                continue;

            EntManager.TryGetComponent<IFFComponent>(grid.Owner, out var iff);

            // Hide it entirely.
            if (iff != null && (iff.Flags & IFFFlags.Hide) != 0x0)
            {
                continue;
            }

            shown.Add(grid.Owner);
            var name = metaQuery.GetComponent(grid.Owner).EntityName;

            if (name == string.Empty)
                name = Loc.GetString("shuttle-console-unknown");

            var gridMatrix = _formSystem.GetWorldMatrix(grid.Owner);
            Matrix3.Multiply(in gridMatrix, in parameters.DrawMatrix, out var matty);
            var color = iff?.Color ?? Color.Gold;

            // Others default:
            // Color.FromHex("#FFC000FF")
            // Hostile default: Color.Firebrick

            if (ShowIFF && (iff == null && IFFComponent.ShowIFFDefault || (iff.Flags & IFFFlags.HideLabel) == 0x0))
            {
                var gridBounds = grid.LocalAABB;
                Label label;

                if (!_iffControls.TryGetValue(grid.Owner, out var control))
                {
                    label = new Label()
                    {
                        HorizontalAlignment = Control.HAlignment.Left,
                    };

                    _iffControls[grid.Owner] = label;
                    Radar.AddChild(label);
                }
                else
                {
                    label = (Label) control;
                }

                var gridBody = bodyQuery.GetComponent(grid.Owner);
                label.FontColorOverride = color;
                var gridCentre = matty.Transform(gridBody.LocalCenter);
                gridCentre.Y = -gridCentre.Y;
                var distance = gridCentre.Length();

                // y-offset the control to always render below the grid (vertically)
                var yOffset = Math.Max(gridBounds.Height, gridBounds.Width) * MinimapScale / 1.8f / Radar.UIScale;

                // The actual position in the UI. We offset the matrix position to render it off by half its width
                // plus by the offset.
                var uiPosition = ScalePosition(gridCentre) / Radar.UIScale - new Vector2(label.Width / 2f, -yOffset);

                // Look this is uggo so feel free to cleanup. We just need to clamp the UI position to within the viewport.
                uiPosition = new Vector2(Math.Clamp(uiPosition.X, 0f, Radar.Width - label.Width),
                    Math.Clamp(uiPosition.Y, 10f, Radar.Height - label.Height));

                label.Visible = true;
                label.Text = Loc.GetString("shuttle-console-iff-label", ("name", name),
                    ("distance", $"{distance:0.0}"));
                LayoutContainer.SetPosition(label, uiPosition);
            }
            else
            {
                ClearLabel(grid.Owner);
            }

            // Detailed view
            DrawGrid(handle, matty, grid, color);
        }

        foreach (var (ent, _) in _iffControls)
        {
            if (shown.Contains(ent))
                continue;
            ClearLabel(ent);
        }
    }

    public override void OnClear()
    {
        foreach (var (_, label) in _iffControls)
        {
            label.Dispose();
        }

        _iffControls.Clear();
    }

    private void ClearLabel(EntityUid uid)
    {
        if (!_iffControls.TryGetValue(uid, out var label))
            return;
        label.Dispose();
        _iffControls.Remove(uid);
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, MapGridComponent grid, Color color)
    {
        if (_cachedGridInfo.TryGetValue(grid, out var info))
        {
            Vector2[] edges = new Vector2[info.Edges.Count];
            int edgesLength = 0;
            for (int i = 0; i < info.Edges.Count - 1; i += 2)
            {
                Vector2 start = info.Edges[i];
                Vector2 end = info.Edges[i + 1];
                start = matrix.Transform(start);
                end = matrix.Transform(end);
                start = ScalePosition(new Vector2(start.X, -start.Y));
                end = ScalePosition(new Vector2(end.X, -end.Y));
                edges[edgesLength] = start;
                edges[edgesLength + 1] = end;
                edgesLength += 2;
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.LineList, edges, color);
        }
    }

    private List<Vector2> GetGridEdges(MapGridComponent grid)
    {
        var enumerator = _mapSystem.GetAllTilesEnumerator(grid.Owner, grid);
        var edges = new List<Vector2>();

        while (enumerator.MoveNext(out var tileRef))
        {
            // TODO: Short-circuit interior chunk nodes
            // This can be optimised a lot more if required.
            Vector2? tileVec = null;

            // Iterate edges and see which we can draw
            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var dirVec = dir.AsDir().ToIntVec();

                if (!_mapSystem.GetTileRef(grid.Owner, grid, tileRef.Value.GridIndices + dirVec).Tile.IsEmpty)
                    continue;

                Vector2 start;
                Vector2 end;
                tileVec ??= (Vector2) tileRef.Value.GridIndices * grid.TileSize;

                // Draw line
                // Could probably rotate this but this might be faster?
                switch (dir)
                {
                    case DirectionFlag.South:
                        start = tileVec.Value;
                        end = tileVec.Value + new Vector2(grid.TileSize, 0f);
                        break;
                    case DirectionFlag.East:
                        start = tileVec.Value + new Vector2(grid.TileSize, 0f);
                        end = tileVec.Value + new Vector2(grid.TileSize, grid.TileSize);
                        break;
                    case DirectionFlag.North:
                        start = tileVec.Value + new Vector2(grid.TileSize, grid.TileSize);
                        end = tileVec.Value + new Vector2(0f, grid.TileSize);
                        break;
                    case DirectionFlag.West:
                        start = tileVec.Value + new Vector2(0f, grid.TileSize);
                        end = tileVec.Value;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                edges.Add(start);
                edges.Add(end);
            }
        }

        return edges;
    }

    private float GetCacheTimeExpiration(EntityUid gridUid)
    {
        if (EntManager.HasComponent<ShipEventFactionMarkerComponent>(gridUid))
            return 0;
        if ((_formSystem.GetWorldPosition(ParentUid) - _formSystem.GetWorldPosition(gridUid)).Length() < 50)
            return 0;
        return 30;
    }

    private sealed class GridInfo
    {
        public float NextUpdateTime;
        public List<Vector2> Edges;

        public GridInfo(float nextUpdateTime, List<Vector2> edges)
        {
            NextUpdateTime = nextUpdateTime;
            Edges = edges;
        }
    }
}
