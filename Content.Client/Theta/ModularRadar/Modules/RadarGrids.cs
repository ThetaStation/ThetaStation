using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarGrids : RadarModule
{
    /// <summary>
    /// Shows a label on each radar object.
    /// </summary>
    private Dictionary<EntityUid, Control> _iffControls = new();

    public bool ShowIFF { get; set; } = true;

    public RadarGrids(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        var fixturesQuery = EntManager.GetEntityQuery<FixturesComponent>();
        var xformQuery = EntManager.GetEntityQuery<TransformComponent>();
        var bodyQuery = EntManager.GetEntityQuery<PhysicsComponent>();
        var metaQuery = EntManager.GetEntityQuery<MetaDataComponent>();

        // Draw our grid in detail
        var ourGridId = ParentCoordinates!.Value.GetGridUid(EntManager);
        if (EntManager.TryGetComponent<MapGridComponent>(ourGridId, out var ourGrid) &&
            fixturesQuery.TryGetComponent(ourGridId, out var ourFixturesComp))
        {
            var ourGridMatrix = xformQuery.GetComponent(ourGridId.Value).WorldMatrix;

            Matrix3.Multiply(in ourGridMatrix, in parameters.DrawMatrix, out var matrix);

            DrawGrid(handle, parameters, matrix, ourGrid, Color.MediumSpringGreen);
        }

        var mapPosition = ParentCoordinates.Value.ToMap(EntManager);

        var shown = new HashSet<EntityUid>();
        // Draw other grids... differently
        foreach (var grid in MapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - MaxRadarRange, mapPosition.Position + MaxRadarRange)))
        {
            if (grid.Owner == ourGridId || !fixturesQuery.HasComponent(grid.Owner))
                continue;

            var gridBody = bodyQuery.GetComponent(grid.Owner);
            if (gridBody.Mass < 10f)
            {
                ClearLabel(grid.Owner);
                continue;
            }

            EntManager.TryGetComponent<IFFComponent>(grid.Owner, out var iff);

            // Hide it entirely.
            if (iff != null &&
                (iff.Flags & IFFFlags.Hide) != 0x0)
            {
                continue;
            }

            shown.Add(grid.Owner);
            var name = metaQuery.GetComponent(grid.Owner).EntityName;

            if (name == string.Empty)
                name = Loc.GetString("shuttle-console-unknown");

            var gridXform = xformQuery.GetComponent(grid.Owner);
            var gridMatrix = gridXform.WorldMatrix;
            Matrix3.Multiply(in gridMatrix, in parameters.DrawMatrix, out var matty);
            var color = iff?.Color ?? Color.Gold;

            // Others default:
            // Color.FromHex("#FFC000FF")
            // Hostile default: Color.Firebrick

            if (ShowIFF &&
                (iff == null && IFFComponent.ShowIFFDefault ||
                 (iff.Flags & IFFFlags.HideLabel) == 0x0))
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

                label.FontColorOverride = color;
                var gridCentre = matty.Transform(gridBody.LocalCenter);
                gridCentre.Y = -gridCentre.Y;
                var distance = gridCentre.Length;

                // y-offset the control to always render below the grid (vertically)
                var yOffset = Math.Max(gridBounds.Height, gridBounds.Width) * MinimapScale / 1.8f / Radar.UIScale;

                // The actual position in the UI. We offset the matrix position to render it off by half its width
                // plus by the offset.
                var uiPosition = ScalePosition(gridCentre) / Radar.UIScale - new Vector2(label.Width / 2f, -yOffset);

                // Look this is uggo so feel free to cleanup. We just need to clamp the UI position to within the viewport.
                uiPosition = new Vector2(Math.Clamp(uiPosition.X, 0f, Radar.Width - label.Width),
                    Math.Clamp(uiPosition.Y, 10f, Radar.Height - label.Height));

                label.Visible = true;
                label.Text = Loc.GetString("shuttle-console-iff-label", ("name", name), ("distance", $"{distance:0.0}"));
                LayoutContainer.SetPosition(label, uiPosition);
            }
            else
            {
                ClearLabel(grid.Owner);
            }

            // Detailed view
            DrawGrid(handle, parameters, matty, grid, color);
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

    private void DrawGrid(DrawingHandleScreen handle, Parameters parameters, Matrix3 matrix, MapGridComponent grid, Color color)
    {
        var rator = grid.GetAllTilesEnumerator();
        var edges = new ValueList<Vector2>();

        while (rator.MoveNext(out var tileRef))
        {
            // TODO: Short-circuit interior chunk nodes
            // This can be optimised a lot more if required.
            Vector2? tileVec = null;

            // Iterate edges and see which we can draw
            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var dirVec = dir.AsDir().ToIntVec();

                if (!grid.GetTileRef(tileRef.Value.GridIndices + dirVec).Tile.IsEmpty)
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

                var adjustedStart = matrix.Transform(start);
                var adjustedEnd = matrix.Transform(end);

                if (adjustedStart.Length > ActualRadarRange || adjustedEnd.Length > ActualRadarRange)
                    continue;

                start = ScalePosition(new Vector2(adjustedStart.X, -adjustedStart.Y));
                end = ScalePosition(new Vector2(adjustedEnd.X, -adjustedEnd.Y));

                edges.Add(start);
                edges.Add(end);
            }
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, edges.Span, color);
    }
}
