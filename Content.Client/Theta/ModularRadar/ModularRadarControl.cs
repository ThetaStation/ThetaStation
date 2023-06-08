using System.Diagnostics.CodeAnalysis;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.Theta.ModularRadar;

/// <summary>
/// Displays nearby grids inside of a control.
/// </summary>
public abstract class ModularRadarControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private const float GridLinesDistance = 32f;

    /// <summary>
    /// Used to transform all of the radar objects. Typically is a shuttle console parented to a grid.
    /// </summary>
    private EntityCoordinates? _coordinates;

    private Angle? _rotation;

    protected readonly List<RadarModule> Modules = new();

    public ModularRadarControl() : base(64f, 256f, 256f)
    {
    }

    public bool TryGetModule<T>([NotNullWhen(true)] out T? module) where T : RadarModule
    {
        foreach (var radarModule in Modules)
        {
            if (radarModule is T module1)
            {
                module = module1;
                return true;
            }
        }

        module = default;
        return false;
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _coordinates = coordinates;
        _rotation = angle;

        foreach (var module in Modules)
        {
            module.SetMatrix(coordinates, angle);
        }
    }

    public void UpdateState(BoundUserInterfaceState ls)
    {
        if (ls is RadarConsoleBoundInterfaceState state)
        {
            WorldMaxRange = state.MaxRange;

            if (WorldMaxRange < WorldRange)
            {
                ActualRadarRange = WorldMaxRange;
            }

            if (WorldMaxRange < WorldMinRange)
                WorldMinRange = WorldMaxRange;

            ActualRadarRange = Math.Clamp(ActualRadarRange, WorldMinRange, WorldMaxRange);
        }

        foreach (var module in Modules)
        {
            module.UpdateState(ls);
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var fakeAA = new Color(0.08f, 0.08f, 0.08f);

        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius, Color.Black);

        // No data
        if (_coordinates == null || _rotation == null)
        {
            Clear();
            return;
        }

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

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        var mapPosition = _coordinates.Value.ToMap(_entManager);

        if (mapPosition.MapId == MapId.Nullspace ||
            !xformQuery.TryGetComponent(_coordinates.Value.EntityId, out var xform))
        {
            Clear();
            return;
        }

        var offsetMatrix = Matrix3.CreateInverseTransform(
            mapPosition.Position,
            xform.WorldRotation + _rotation.Value);

        var parameters = new RadarModule.Parameters(
            drawMatrix: offsetMatrix
        );

        foreach (var module in Modules)
        {
            module.Draw(handle, parameters);
        }
    }

    private void Clear()
    {
        foreach (var module in Modules)
        {
            module.OnClear();
        }
    }

    public int GetMinimapMargin()
    {
        return MinimapMargin;
    }

    public int GetUIDisplayRadius()
    {
        return UIDisplayRadius;
    }

    public float GetActualRadarRange()
    {
        return ActualRadarRange;
    }
}
