using System.Linq;
using Content.Client.Theta.ShipEvent;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Content.Shared.Shuttles.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Displays nearby grids inside of a control.
/// </summary>
public sealed class RadarControl : Control
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const float ScrollSensitivity = 8f;
    private const float GridLinesDistance = 32f;

    /// <summary>
    /// Used to transform all of the radar objects. Typically is a shuttle console parented to a grid.
    /// </summary>
    private EntityCoordinates? _coordinates;

    private Angle? _rotation;

    private float _radarMinRange = SharedRadarConsoleSystem.DefaultMinRange;
    private float _radarMaxRange = SharedRadarConsoleSystem.DefaultMaxRange;
    public float RadarRange { get; private set; } = SharedRadarConsoleSystem.DefaultMinRange;

    /// <summary>
    /// We'll lerp between the radarrange and actual range
    /// </summary>
    private float _actualRadarRange = SharedRadarConsoleSystem.DefaultMinRange;

    /// <summary>
    /// Controls the maximum distance that IFF labels will display.
    /// </summary>
    public float MaxRadarRange { get; private set; } = 256f * 10f;

    private int MinimapRadius => (int) Math.Min(Size.X, Size.Y) / 2;
    private Vector2 MidPoint => (Size / 2) * UIScale;
    private int SizeFull => (int) (MinimapRadius * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    private float MinimapScale => RadarRange != 0 ? ScaledMinimapRadius / RadarRange : 0f;

    /// <summary>
    /// Shows a label on each radar object.
    /// </summary>
    private Dictionary<EntityUid, Control> _iffControls = new();

    private Dictionary<EntityUid, List<DockingInterfaceState>> _docks = new();

    private List<MobInterfaceState> _mobs = new();

    private List<ProjectilesInterfaceState> _projectiles = new();

    private List<CannonInformationInterfaceState> _cannons = new();

    public bool ShowIFF { get; set; } = true;
    public bool ShowDocks { get; set; } = true;

    /// <summary>
    /// Currently hovered docked to show on the map.
    /// </summary>
    public EntityUid? HighlightedDock;

    public Action<float>? OnRadarRangeChanged;

    private List<EntityUid> _controlledCannons = new();

    private int _nextMouseHandle;

    private const int _mouseCD = 20;

    /// <summary>
    /// Raised if the user left-clicks on the radar control with the relevant entitycoordinates.
    /// </summary>
    public Action<EntityCoordinates>? OnRadarClick;

    public RadarControl()
    {
        IoCManager.InjectDependencies(this);
        MinSize = (SizeFull, SizeFull);
        RectClipContent = true;

        OnKeyBindDown += StartFiring;
        OnKeyBindUp += StopFiring;
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _coordinates = coordinates;
        _rotation = angle;
    }

    public void UpdateState(RadarConsoleBoundInterfaceState ls)
    {
        _radarMaxRange = ls.MaxRange;

        if (_radarMaxRange < _radarMinRange)
            _radarMinRange = _radarMaxRange;

        _actualRadarRange = Math.Clamp(_actualRadarRange, _radarMinRange, _radarMaxRange);

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
        _controlledCannons = _cannons
            .Where(i => i.IsControlling)
            .Select(i => i.Uid)
            .ToList();
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (_coordinates == null || _rotation == null || args.Function != EngineKeyFunctions.UIClick ||
            OnRadarClick == null)
        {
            return;
        }

        var a = InverseScalePosition(args.RelativePosition);
        var relativeWorldPos = new Vector2(a.X, -a.Y);
        relativeWorldPos = _rotation.Value.RotateVec(relativeWorldPos);
        var coords = _coordinates.Value.Offset(relativeWorldPos);
        OnRadarClick?.Invoke(coords);
    }

    /// <summary>
    /// Gets the entitycoordinates of where the mouseposition is, relative to the control.
    /// </summary>
    [PublicAPI]
    public EntityCoordinates GetMouseCoordinates(ScreenCoordinates screen)
    {
        if (_coordinates == null || _rotation == null)
        {
            return EntityCoordinates.Invalid;
        }

        var pos = screen.Position / UIScale - GlobalPosition;

        var a = InverseScalePosition(pos);
        var relativeWorldPos = new Vector2(a.X, -a.Y);
        relativeWorldPos = _rotation.Value.RotateVec(relativeWorldPos);
        var coords = _coordinates.Value.Offset(relativeWorldPos);
        return coords;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (_nextMouseHandle < _mouseCD)
        {
            _nextMouseHandle++;
            return;
        }

        if (_controlledCannons.Count == 0)
            return;

        _nextMouseHandle = 0;
        RotateCannons(args.RelativePosition);
        args.Handle();
    }

    private void StartFiring(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.Use)
            return;

        if (_controlledCannons.Count == 0)
            return;

        var coordinates = RotateCannons(args.RelativePosition);

        var player = _player.LocalPlayer?.ControlledEntity;
        if (player == null)
            return;

        var ev = new StartCannonFiringEvent(coordinates, player.Value);
        foreach (var entityUid in _controlledCannons)
        {
            _entManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }

        args.Handle();
    }

    private void StopFiring(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.Use)
            return;

        if (_controlledCannons.Count == 0)
            return;

        var ev = new StopCannonFiringEventEvent();
        foreach (var entityUid in _controlledCannons)
        {
            _entManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }
    }

    private Vector2 RotateCannons(Vector2 mouseRelativePosition)
    {
        var offsetMatrix = GetOffsetMatrix();
        var relativePositionToCoordinates = RelativePositionToCoordinates(mouseRelativePosition, offsetMatrix);
        var player = _player.LocalPlayer?.ControlledEntity;
        if (player == null)
            return relativePositionToCoordinates;
        foreach (var entityUid in _controlledCannons)
        {
            var ev = new RotateCannonEvent(relativePositionToCoordinates, player.Value);
            _entManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }

        return relativePositionToCoordinates;
    }

    private Vector2 RelativePositionToCoordinates(Vector2 pos, Matrix3 matrix)
    {
        var removeScale = InverseScalePosition(pos);
        removeScale.Y = -removeScale.Y;
        return matrix.Transform(removeScale);
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

    private Matrix3 GetOffsetMatrix()
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

            RadarRange += (float) Math.Clamp(diff, -lerpRate * MathF.Abs(diff) * _timing.FrameTime.TotalSeconds,
                lerpRate * MathF.Abs(diff) * _timing.FrameTime.TotalSeconds);
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
        if (_entManager.TryGetComponent<MapGridComponent>(ourGridId, out var ourGrid) &&
            fixturesQuery.TryGetComponent(ourGridId, out var ourFixturesComp))
        {
            var transformGridComp = xformQuery.GetComponent(ourGridId.Value);
            var ourGridMatrix = transformGridComp.WorldMatrix;
            var ourGridFixtures = fixturesQuery.GetComponent(ourGridId.Value);

            Matrix3.Multiply(in ourGridMatrix, in offsetMatrix, out var matrix);

            DrawGrid(handle, matrix, ourFixturesComp, ourGrid, Color.MediumSpringGreen, true);
            DrawDocks(handle, ourGridId.Value, matrix);

            var worldRot = transformGridComp.WorldRotation;
            // Get the positive reduced angle.
            var displayRot = -worldRot.Reduced();

            var gridPhysic = bodyQuery.GetComponent(ourGridId.Value);
            var gridVelocity = displayRot.RotateVec(gridPhysic.LinearVelocity);

            DrawVelocityArrow(handle, gridVelocity);
        }

        var shown = new HashSet<EntityUid>();

        // Draw other grids... differently
        foreach (var grid in _mapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - MaxRadarRange, mapPosition.Position + MaxRadarRange)))
        {
            if (grid.Owner == ourGridId || !fixturesQuery.TryGetComponent(grid.Owner, out var fixturesComp))
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
            var gridMatrix = gridXform.WorldMatrix;
            Matrix3.Multiply(in gridMatrix, in offsetMatrix, out var matty);
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
                label.Text = Loc.GetString("shuttle-console-iff-label", ("name", name),
                    ("distance", $"{distance:0.0}"));
                LayoutContainer.SetPosition(label, uiPosition);
            }
            else
            {
                ClearLabel(grid.Owner);
            }

            // Detailed view
            DrawGrid(handle, matty, fixturesComp, grid, color, true);

            DrawDocks(handle, grid.Owner, matty);
        }

        DrawProjectiles(handle, offsetMatrix);

        DrawMobs(handle, offsetMatrix);

        DrawCannons(handle, offsetMatrix);

        var offset = _coordinates.Value.Position;
        var invertedPosition = _coordinates.Value.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;
        // Don't need to transform the InvWorldMatrix again as it's already offset to its position.

        // Draw radar position on the station
        handle.DrawCircle(ScalePosition(invertedPosition), 5f, Color.Lime);

        foreach (var (ent, _) in _iffControls)
        {
            if (shown.Contains(ent))
                continue;
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
        if (!_iffControls.TryGetValue(uid, out var label))
            return;
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
                matrix.Transform(position + angle.RotateVec(new Vector2(-projectileSize / 2, 0))),
                matrix.Transform(position + angle.RotateVec(new Vector2(projectileSize / 2, 0))),
                matrix.Transform(position + angle.RotateVec(new Vector2(0, -projectileSize))),
                matrix.Transform(position + angle.RotateVec(new Vector2(-projectileSize / 2, 0))),
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

    private void DrawVelocityArrow(DrawingHandleScreen handle, Vector2 gridVelocity)
    {
        const float arrowSize = 3f;

        var (x, y) = (gridVelocity.X, gridVelocity.Y);
        if (x == 0f && y == 0f)
            return;

        var angle = Angle.FromWorldVec(gridVelocity);
        var verts = new[]
        {
            gridVelocity + angle.RotateVec(new Vector2(-arrowSize / 2, arrowSize / 4)),
            gridVelocity + angle.RotateVec(new Vector2(0, -arrowSize / 2 - arrowSize / 4)),
            gridVelocity + angle.RotateVec(new Vector2(arrowSize / 2, arrowSize / 4)),
            gridVelocity + angle.RotateVec(new Vector2(arrowSize / 4, arrowSize / 4)),
            gridVelocity + angle.RotateVec(new Vector2(arrowSize / 4, arrowSize)),
            gridVelocity + angle.RotateVec(new Vector2(-arrowSize / 4, arrowSize)),
            gridVelocity + angle.RotateVec(new Vector2(-arrowSize / 4, arrowSize / 4)),
            gridVelocity + angle.RotateVec(new Vector2(-arrowSize / 2, arrowSize / 4)),
        };
        for (var i = 0; i < verts.Length; i++)
        {
            var vert = verts[i];
            vert.Y = -vert.Y;
            verts[i] = ScalePosition(vert);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, Color.White);
    }

    // transform components on the client should be enough to get the angle
    private void DrawCannons(DrawingHandleScreen handle, Matrix3 matrix)
    {
        const float cannonSize = 3f;
        foreach (var cannon in _cannons)
        {
            var position = cannon.Coordinates.ToMapPos(_entManager);
            var angle = cannon.Angle;
            var color = cannon.Color;

            var hsvColor = Color.ToHsv(color);

            const float additionalDegreeCoeff = 20f / 360f;

            // X is hue
            var hueOffset = hsvColor.X - (hsvColor.X * cannon.Ammo / cannon.Capacity);
            hsvColor.X = Math.Max(hsvColor.X-hueOffset+additionalDegreeCoeff, additionalDegreeCoeff);

            color = Color.FromHsv(hsvColor);

            var verts = new[]
            {
                matrix.Transform(position + angle.RotateVec(new Vector2(-cannonSize / 2, cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(0, -cannonSize / 2 - cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(cannonSize / 2, cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(-cannonSize / 2, cannonSize / 4))),
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
        if (!ShowDocks)
            return;

        const float DockScale = 1f;

        if (_docks.TryGetValue(uid, out var docks))
        {
            foreach (var state in docks)
            {
                var ent = state.Entity;
                var position = state.Coordinates.Position;
                var uiPosition = matrix.Transform(position);

                if (uiPosition.Length > RadarRange - DockScale)
                    continue;

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

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color.WithAlpha(0.8f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
            }
        }
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, FixturesComponent fixturesComp,
        MapGridComponent grid, Color color, bool drawInterior)
    {
        var rator = grid.GetAllTilesEnumerator();
        var edges = new ValueList<Vector2>();
        var tileTris = new ValueList<Vector2>();

        if (drawInterior)
        {
            var interiorTris = new ValueList<Vector2>();
            // TODO: Engine pr
            Span<Vector2> verts = new Vector2[8];

            foreach (var fixture in fixturesComp.Fixtures.Values)
            {
                var invalid = false;
                var poly = (PolygonShape) fixture.Shape;

                for (var i = 0; i < poly.VertexCount; i++)
                {
                    var vert = poly.Vertices[i];
                    vert = new Vector2(MathF.Round(vert.X), MathF.Round(vert.Y));

                    vert = matrix.Transform(vert);

                    if (vert.Length > RadarRange)
                    {
                        invalid = true;
                        break;
                    }

                    verts[i] = vert;
                }

                if (invalid)
                    continue;

                Vector2 AdjustedVert(Vector2 vert)
                {
                    if (vert.Length > RadarRange)
                    {
                        vert = vert.Normalized * RadarRange;
                    }

                    vert.Y = -vert.Y;
                    return ScalePosition(vert);
                }

                interiorTris.Add(AdjustedVert(verts[0]));
                interiorTris.Add(AdjustedVert(verts[1]));
                interiorTris.Add(AdjustedVert(verts[3]));

                interiorTris.Add(AdjustedVert(verts[1]));
                interiorTris.Add(AdjustedVert(verts[2]));
                interiorTris.Add(AdjustedVert(verts[3]));
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, interiorTris.Span, color.WithAlpha(0.05f));
        }

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

                if (adjustedStart.Length > RadarRange || adjustedEnd.Length > RadarRange)
                    continue;

                start = ScalePosition(new Vector2(adjustedStart.X, -adjustedStart.Y));
                end = ScalePosition(new Vector2(adjustedEnd.X, -adjustedEnd.Y));

                edges.Add(start);
                edges.Add(end);
            }
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, tileTris.Span, color.WithAlpha(0.05f));
        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, edges.Span, color);
    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPoint;
    }

    private Vector2 InverseScalePosition(Vector2 value)
    {
        return (value - MidPoint) / MinimapScale;
    }
}
