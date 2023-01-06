using Content.Client.Stylesheets;
using Content.Client.Theta.ShipEvent;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Displays nearby grids inside of a control.
/// </summary>
public abstract class RadarControl : Control
{
    [Dependency] protected readonly IEntityManager _entManager = default!;
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] protected readonly IMapManager _mapManager = default!;

    private const float ScrollSensitivity = 8f;
    private const float GridLinesDistance = 32f;

    /// <summary>
    /// Used to transform all of the radar objects. Typically is a shuttle console parented to a grid.
    /// </summary>
    protected EntityCoordinates? _coordinates;

    protected Angle? _rotation;

    protected float _radarMinRange = 64f;
    protected float _radarMaxRange = 256f;
    public float RadarRange { get; private set; } = 256f;

    /// <summary>
    /// We'll lerp between the radarrange and actual range
    /// </summary>
    protected float _actualRadarRange = 256f;

    /// <summary>
    /// Controls the maximum distance that IFF labels will display.
    /// </summary>
    public float MaxRadarRange { get; private set; } = 256f * 10f;

    private int MinimapRadius => (int) Math.Min(Size.X, Size.Y) / 2;
    protected Vector2 MidPoint => Size / 2;
    private int SizeFull => (int) (MinimapRadius * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    protected float MinimapScale => RadarRange != 0 ? ScaledMinimapRadius / RadarRange : 0f;

    /// <summary>
    /// Shows a label on each radar object.
    /// </summary>
    protected Dictionary<EntityUid, Control> _iffControls = new();

    protected Dictionary<EntityUid, List<DockingInterfaceState>> _docks = new();

    protected List<MobInterfaceState> _mobs = new();

    protected List<ProjectilesInterfaceState> _projectiles = new();

    protected List<CannonInterfaceState> _cannons = new();

    public bool ShowIFF { get; set; } = true;
    public bool ShowDocks { get; set; } = true;

    /// <summary>
    /// Currently hovered docked to show on the map.
    /// </summary>
    public EntityUid? HighlightedDock;

    public Action<float>? OnRadarRangeChanged;

    public RadarControl()
    {
        IoCManager.InjectDependencies(this);
        MinSize = (SizeFull, SizeFull);
        RectClipContent = true;
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _coordinates = coordinates;
        _rotation = angle;
    }

    public void UpdateState(RadarConsoleBoundInterfaceState ls)
    {
        _radarMaxRange = ls.MaxRange;

        if (_radarMaxRange < RadarRange)
        {
            _actualRadarRange = _radarMaxRange;
        }

        if (_radarMaxRange < _radarMinRange)
            _radarMinRange = _radarMaxRange;

        _docks.Clear();

        foreach (var state in ls.Docks)
        {
            var coordinates = state.Coordinates;
            var grid = _docks.GetOrNew(coordinates.EntityId);
            grid.Add(state);
        }

        _mobs = ls.MobsAround;
        _projectiles = ls.Projectiles;

        _cannons = ls.Cannons;

    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        AddRadarRange(-args.Delta.Y * 1f / ScrollSensitivity * RadarRange);
    }

    public void AddRadarRange(float value)
    {
        _actualRadarRange = Math.Clamp(_actualRadarRange + value, _radarMinRange, _radarMaxRange);
    }

    protected Matrix3 GetOffsetMatrix()
    {
        if (_coordinates == null || _rotation == null)
            return Matrix3.Zero;

        var mapPosition = _coordinates.Value.ToMap(_entManager);
        if (mapPosition.MapId == MapId.Nullspace)
            return Matrix3.Zero;

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        if (!xformQuery.TryGetComponent(_coordinates.Value.EntityId, out var xform))
            return Matrix3.Zero;

        var offsetMatrix = Matrix3.CreateTransform(
            mapPosition.Position,
            xform.WorldRotation - _rotation.Value);
        return offsetMatrix;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!_actualRadarRange.Equals(RadarRange))
        {
            var diff = _actualRadarRange - RadarRange;
            var lerpRate = 10f;

            RadarRange += (float) Math.Clamp(diff, -lerpRate * MathF.Abs(diff) * _timing.FrameTime.TotalSeconds, lerpRate * MathF.Abs(diff) * _timing.FrameTime.TotalSeconds);
            OnRadarRangeChanged?.Invoke(RadarRange);
        }

        var fakeAA = new Color(0.08f, 0.08f, 0.08f);

        handle.DrawCircle((MidPoint.X, MidPoint.Y), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((MidPoint.X, MidPoint.Y), ScaledMinimapRadius, Color.Black);

        // No data
        if (_coordinates == null || _rotation == null)
        {
            Clear();
            return;
        }

        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(RadarRange / GridLinesDistance);

        for (var i = 1; i < gridLinesEquatorial + 1; i++)
        {
            handle.DrawCircle((MidPoint.X, MidPoint.Y), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine((MidPoint.X, MidPoint.Y) - aExtent, (MidPoint.X, MidPoint.Y) + aExtent, gridLines);
        }

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var fixturesQuery = _entManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = _entManager.GetEntityQuery<PhysicsComponent>();

        var mapPosition = _coordinates.Value.ToMap(_entManager);
        var offsetMatrix = GetOffsetMatrix();
        if (offsetMatrix.Equals(Matrix3.Zero))
        {
            Clear();
            return;
        }

        offsetMatrix = offsetMatrix.Invert();

        // Draw our grid in detail
        var ourGridId = _coordinates.Value.GetGridUid(_entManager);
        if (ourGridId != null)
        {
            var ourGridMatrix = xformQuery.GetComponent(ourGridId.Value).WorldMatrix;
            var ourGridFixtures = fixturesQuery.GetComponent(ourGridId.Value);

            Matrix3.Multiply(in ourGridMatrix, in offsetMatrix, out var matrix);

            // Draw our grid; use non-filled boxes so it doesn't look awful.
            DrawGrid(handle, matrix, ourGridFixtures, Color.Yellow);

            DrawDocks(handle, ourGridId.Value, matrix);
        }

        var shown = new HashSet<EntityUid>();

        // Draw other grids... differently
        foreach (var grid in _mapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - MaxRadarRange, mapPosition.Position + MaxRadarRange)))
        {
            if (grid.Owner == ourGridId)
                continue;

            var gridBody = bodyQuery.GetComponent(grid.Owner);
            if (gridBody.Mass < 10f)
            {
                ClearLabel(grid.Owner);
                continue;
            }

            _entManager.TryGetComponent<IFFComponent>(grid.Owner, out var iff);

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
            var gridFixtures = fixturesQuery.GetComponent(grid.Owner);
            var gridMatrix = gridXform.WorldMatrix;
            Matrix3.Multiply(in gridMatrix, in offsetMatrix, out var matty);
            var color = iff?.Color ?? IFFComponent.IFFColor;

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
                        HorizontalAlignment = HAlignment.Left,
                    };

                    _iffControls[grid.Owner] = label;
                    AddChild(label);
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
                var yOffset = Math.Max(gridBounds.Height, gridBounds.Width) * MinimapScale / 1.8f / UIScale;

                // The actual position in the UI. We offset the matrix position to render it off by half its width
                // plus by the offset.
                var uiPosition = ScalePosition(gridCentre) / UIScale - new Vector2(label.Width / 2f, -yOffset);

                // Look this is uggo so feel free to cleanup. We just need to clamp the UI position to within the viewport.
                uiPosition = new Vector2(Math.Clamp(uiPosition.X, 0f, Width - label.Width),
                    Math.Clamp(uiPosition.Y, 10f, Height - label.Height));

                label.Visible = true;
                label.Text = Loc.GetString("shuttle-console-iff-label", ("name", name), ("distance", $"{distance:0.0}"));
                LayoutContainer.SetPosition(label, uiPosition);
            }
            else
            {
                ClearLabel(grid.Owner);
            }

            // Detailed view
            DrawGrid(handle, matty, gridFixtures, color);

            DrawDocks(handle, grid.Owner, matty);
        }

        DrawProjectiles(handle, offsetMatrix);

        DrawMobs(handle, offsetMatrix);

        DrawCannons(handle, offsetMatrix, xformQuery);

        var offset = _coordinates.Value.Position;
        var invertedPosition = _coordinates.Value.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;
        // Don't need to transform the InvWorldMatrix again as it's already offset to its position.

        // Draw radar position on the station
        handle.DrawCircle(ScalePosition(invertedPosition), 5f, Color.Lime);

        foreach (var (ent, _) in _iffControls)
        {
            if (shown.Contains(ent)) continue;
            ClearLabel(ent);
        }
    }

    private void Clear()
    {
        foreach (var (_, label) in _iffControls)
        {
            label.Dispose();
        }

        _iffControls.Clear();
    }

    private void ClearLabel(EntityUid uid)
    {
        if (!_iffControls.TryGetValue(uid, out var label)) return;
        label.Dispose();
        _iffControls.Remove(uid);
    }

    private void DrawProjectiles(DrawingHandleScreen handle, Matrix3 matrix)
    {
        const float projectileSize = 1.5f;
        foreach (var state in _projectiles)
        {
            var position = state.Coordinates.ToMapPos(_entManager);
            var angle = state.Angle;
            var color = Color.Brown;

            var verts = new[]
            {
                matrix.Transform(position + angle.RotateVec(new Vector2(-projectileSize/2, 0))),
                matrix.Transform(position + angle.RotateVec(new Vector2(projectileSize/2, 0))),
                matrix.Transform(position + angle.RotateVec(new Vector2(0, -projectileSize))),
                matrix.Transform(position + angle.RotateVec(new Vector2(-projectileSize/2, 0))),
            };
            for (var i = 0; i < verts.Length; i++)
            {
                var vert = verts[i];
                vert.Y = -vert.Y;
                verts[i] = ScalePosition(vert);
            }
            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }

    protected virtual Color GetCannonColor(EntityUid cannon)
    {
        return Color.YellowGreen;
    }

    // transform components on the client should be enough to get the angle
    private void DrawCannons(DrawingHandleScreen handle, Matrix3 matrix, EntityQuery<TransformComponent> entityQuery)
    {
        const float cannonSize = 3f;
        foreach (var cannon in _cannons)
        {
            var position = cannon.Coordinates.ToMapPos(_entManager);
            var angle = cannon.Angle;
            var color = GetCannonColor(cannon.Entity);

            var verts = new[]
            {
                matrix.Transform(position + angle.RotateVec(new Vector2(-cannonSize/2, cannonSize/4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(0, -cannonSize/2 - cannonSize/4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(cannonSize/2, cannonSize/4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(-cannonSize/2, cannonSize/4))),
            };
            for (var i = 0; i < verts.Length; i++)
            {
                var vert = verts[i];
                vert.Y = -vert.Y;
                verts[i] = ScalePosition(vert);
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }

    private void DrawMobs(DrawingHandleScreen handle, Matrix3 matrix)
    {
        foreach (var state in _mobs)
        {
            var position = state.Coordinates.ToMapPos(_entManager);
            var uiPosition = matrix.Transform(position);
            var color = Color.Red;

            uiPosition.Y = -uiPosition.Y;

            handle.DrawCircle(ScalePosition(uiPosition), 3f, color);
        }
    }

    private void DrawDocks(DrawingHandleScreen handle, EntityUid uid, Matrix3 matrix)
    {
        if (!ShowDocks) return;

        const float DockScale = 1.2f;

        if (_docks.TryGetValue(uid, out var docks))
        {
            foreach (var state in docks)
            {
                var ent = state.Entity;
                var position = state.Coordinates.Position;
                var uiPosition = matrix.Transform(position);

                if (uiPosition.Length > RadarRange - DockScale) continue;

                var color = HighlightedDock == ent ? state.HighlightedColor : state.Color;

                uiPosition.Y = -uiPosition.Y;

                var verts = new[]
                {
                    matrix.Transform(position + new Vector2(-DockScale, -DockScale)),
                    matrix.Transform(position + new Vector2(DockScale, -DockScale)),
                    matrix.Transform(position + new Vector2(DockScale, DockScale)),
                    matrix.Transform(position + new Vector2(-DockScale, DockScale)),
                };

                for (var i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    vert.Y = -vert.Y;
                    verts[i] = ScalePosition(vert);
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color);
            }
        }
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, FixturesComponent component, Color color)
    {
        foreach (var (_, fixture) in component.Fixtures)
        {
            // If the fixture has any points out of range we won't draw any of it.
            var invalid = false;
            var poly = (PolygonShape) fixture.Shape;
            var verts = new Vector2[poly.VertexCount + 1];

            for (var i = 0; i < poly.VertexCount; i++)
            {
                var vert = matrix.Transform(poly.Vertices[i]);

                if (vert.Length > RadarRange)
                {
                    invalid = true;
                    break;
                }

                vert.Y = -vert.Y;
                verts[i] = ScalePosition(vert);
            }

            if (invalid) continue;

            // Closed list
            verts[poly.VertexCount] = verts[0];
            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPoint;
    }
}
