using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Client.UserInterface.Systems.Radar.Controls;

public sealed class SimpleRadarControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private const float GridLinesDistance = 32f;

    // new because pendoses has hardcoded all size-related parameters
    private new int UIDisplayRadius = 400;
    private new int MidPoint => (int) (SizeFull / 2);
    private new int SizeFull => (int) (UIDisplayRadius * UIScale);
    private new int ScaledMinimapRadius => (int) (SizeFull / 2);
    private new float MinimapScale => WorldRange != 0 ? ScaledMinimapRadius / WorldRange : 0f;

    public SimpleRadarControl() : base(192f, 192f, 192f)
    {
        SetSize = (SizeFull, SizeFull);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);
        var background = Color.White.WithAlpha(1);
        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius, background);

        // No data
        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(WorldRange / GridLinesDistance);

        for (var i = 1; i < gridLinesEquatorial + 1; i++)
        {
            handle.DrawCircle((MidPoint, MidPoint), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine((MidPoint, MidPoint) - aExtent, (MidPoint, MidPoint) + aExtent, gridLines);
        }

        EntityUid? _owner = null;
        if (_player.LocalPlayer?.ControlledEntity != null)
            _owner = _player.LocalPlayer.ControlledEntity.Value;

        if (_owner == null)
            return;

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var fixturesQuery = _entManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = _entManager.GetEntityQuery<PhysicsComponent>();

        var ownerTransform = xformQuery.GetComponent(_owner.Value);
        var coordinates = ownerTransform.Coordinates;

        var mapPosition = coordinates.ToMap(_entManager);
        var offsetMatrix = GetOffsetMatrix(coordinates);
        if (offsetMatrix.Equals(Matrix3.Zero))
            return;

        offsetMatrix = offsetMatrix.Invert();

        // Draw our grid in detail
        var ourGridId = ownerTransform.GridUid;
        if (ourGridId != null &&
            _entManager.TryGetComponent<MapGridComponent>(ourGridId, out var ourGrid) &&
            fixturesQuery.TryGetComponent(ourGridId, out var ourFixturesComp))
        {
            var transformGridComp = xformQuery.GetComponent(ourGridId.Value);
            var ourGridMatrix = transformGridComp.WorldMatrix;

            Matrix3.Multiply(in ourGridMatrix, in offsetMatrix, out var matrix);

            DrawGrid(handle, matrix, ourFixturesComp, ourGrid, Color.MediumSpringGreen, true);
        }

        // Draw other grids... differently
        foreach (var grid in _mapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - MaxRadarRange, mapPosition.Position + MaxRadarRange)))
        {
            if (grid.Owner == ourGridId || !fixturesQuery.TryGetComponent(grid.Owner, out var fixturesComp))
                continue;

            var gridBody = bodyQuery.GetComponent(grid.Owner);
            if (gridBody.Mass < 10f)
                continue;

            _entManager.TryGetComponent<IFFComponent>(grid.Owner, out var iff);

            // Hide it entirely.
            if (iff != null &&
                (iff.Flags & IFFFlags.Hide) != 0x0)
            {
                continue;
            }

            var name = metaQuery.GetComponent(grid.Owner).EntityName;

            if (name == string.Empty)
                name = Loc.GetString("shuttle-console-unknown");

            var gridXform = xformQuery.GetComponent(grid.Owner);
            var gridMatrix = gridXform.WorldMatrix;
            Matrix3.Multiply(in gridMatrix, in offsetMatrix, out var matty);
            var color = iff?.Color ?? Color.Gold;

            // Detailed view
            DrawGrid(handle, matty, fixturesComp, grid, color, true);
        }

        var offset = coordinates.Position;
        var invertedPosition = coordinates.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;
        // Don't need to transform the InvWorldMatrix again as it's already offset to its position.

        // Draw radar position on the station
        handle.DrawCircle(ScalePosition(invertedPosition), 5f, Color.Lime);
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, FixturesComponent fixturesComp,
        MapGridComponent grid, Color color, bool drawInterior)
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

    private Matrix3 GetOffsetMatrix(EntityCoordinates coordinates)
    {
        var mapPosition = coordinates.ToMap(_entManager!);
        if (mapPosition.MapId == MapId.Nullspace)
            return Matrix3.Zero;

        var offsetMatrix = Matrix3.CreateTransform(
            mapPosition.Position,
            -_eyeManager.CurrentEye.Rotation);
        return offsetMatrix;
    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPoint;
    }
}
