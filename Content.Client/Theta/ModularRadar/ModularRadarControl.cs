using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
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

    public EntityUid OwnerUid;

    protected readonly List<RadarModule> Modules = new();

    public ModularRadarControl(float minRange = 64f, float maxRange = 256f, float range = 256f)
        : base(minRange, maxRange, range)
    {
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        foreach (var module in Modules)
        {
            module.MouseMove(args);
        }
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        foreach (var module in Modules)
        {
            module.OnKeyBindUp(args);
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        foreach (var module in Modules)
        {
            module.OnKeyBindDown(args);
        }
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
    }

    public void SetOwnerUid(EntityUid uid)
    {
        OwnerUid = uid;
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

        var background = GetBackground();
        handle.DrawCircle(new Vector2(MidPoint, MidPoint), ScaledMinimapRadius, background);

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
            handle.DrawCircle(new Vector2(MidPoint, MidPoint), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine(new Vector2(MidPoint, MidPoint) - aExtent, new Vector2(MidPoint, MidPoint) + aExtent, gridLines);
        }

        var offsetMatrix = GetOffsetMatrix();
        if (offsetMatrix.Equals(Matrix3.Zero))
        {
            Clear();
            return;
        }

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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var module in Modules)
        {
            module.OnDispose();
        }
    }

    public Matrix3 GetOffsetMatrix()
    {
        if (_coordinates == null || _rotation == null)
            return Matrix3.Zero;

        var mapPosition = _coordinates.Value.ToMap(_entManager);
        if (mapPosition.MapId == MapId.Nullspace)
            return Matrix3.Zero;

        return Matrix3.CreateInverseTransform(
            mapPosition.Position,
            GetMatrixRotation()
        );
    }

    protected virtual Angle GetMatrixRotation()
    {
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(_coordinates!.Value.EntityId, out var xform))
            return Angle.Zero;

        return xform.WorldRotation + _rotation!.Value;
    }

    protected virtual Color GetBackground()
    {
        return new Color(0.08f, 0.08f, 0.08f);
    }

    // Api to bypass encapsulation without changing MapGridControl

    public float GetActualRadarRange()
    {
        return ActualRadarRange;
    }

    public EntityCoordinates? GetConsoleCoordinates()
    {
        return _coordinates;
    }

    public Angle? GetConsoleRotation()
    {
        return _rotation;
    }
}
