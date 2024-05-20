using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.ShipEvent.UI;
using Pidgin;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Theta.ModularRadar;

/// <summary>
/// Displays nearby grids inside of a control.
/// </summary>
public abstract class ModularRadarControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public readonly Font Font;

    /// <summary>
    /// Used to transform all of the radar objects. Typically is a shuttle console parented to a grid.
    /// </summary>
    private EntityCoordinates? _coordinates;
    private Angle? _rotation;
    public EntityUid OwnerUid;

    protected readonly List<RadarModule> Modules = new();

    public Action? OnParentUidSet;

    public ModularRadarControl(float minRange = 64f, float maxRange = 256f, float range = 256f)
        : base(minRange, maxRange, range)
    {
        Font = IoCManager.Resolve<IResourceCache>().GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 6);
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
        OnParentUidSet?.Invoke();
    }

    public void UpdateState(BoundUserInterfaceState ls)
    {
        switch (ls)
        {
            case RadarConsoleBoundInterfaceState state:
                UpdateMaxRange(state.NavState.MaxRange);
                break;
            case ShieldConsoleBoundsUserInterfaceState shieldState:
                UpdateMaxRange(shieldState.NavState.MaxRange);
                break;
        }

        foreach (var module in Modules)
        {
            module.UpdateState(ls);
        }
    }

    private void UpdateMaxRange(float maxRange)
    {
        WorldMaxRange = maxRange;

        if (WorldMaxRange < WorldRange)
        {
            ActualRadarRange = WorldMaxRange;
        }

        if (WorldMaxRange < WorldMinRange)
            WorldMinRange = WorldMaxRange;

        ActualRadarRange = Math.Clamp(ActualRadarRange, WorldMinRange, WorldMaxRange);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var backing = GetBackground();
        handle.DrawRect(PixelSizeBox, backing);
        DrawCircles(handle);

        // No data
        if (_coordinates == null || _rotation == null)
        {
            Clear();
            return;
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

    protected override void FrameUpdate(FrameEventArgs args)
    {
        foreach (var module in Modules)
        {
            module.FrameUpdate(args);
        }
    }

    private void Clear()
    {
        foreach (var module in Modules)
        {
            module.OnClear();
        }
    }

    private void DrawCircles(DrawingHandleScreen handle)
    {
        // Equatorial lines
        var gridLines = Color.LightGray.WithAlpha(0.01f);

        // Each circle is this x distance of the last one.
        const float EquatorialMultiplier = 2f;

        var minDistance = MathF.Pow(EquatorialMultiplier, EquatorialMultiplier * 1.5f);
        var maxDistance = MathF.Pow(2f, EquatorialMultiplier * 6f);
        var cornerDistance = MathF.Sqrt(WorldRange * WorldRange + WorldRange * WorldRange);

        var origin = ScalePosition(-new Vector2(Offset.X, -Offset.Y));

        for (var radius = minDistance; radius <= maxDistance; radius *= EquatorialMultiplier)
        {
            if (radius > cornerDistance)
                continue;

            var color = Color.ToSrgb(gridLines).WithAlpha(0.05f);
            var scaledRadius = MinimapScale * radius;
            var text = $"{radius:0}m";
            var textDimensions = handle.GetDimensions(Font, text, UIScale);

            handle.DrawCircle(origin, scaledRadius, color, false);
            handle.DrawString(Font, ScalePosition(new Vector2(0f, -radius)) - new Vector2(0f, textDimensions.Y), text, UIScale, color);
        }

        const int gridLinesRadial = 8;

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            // TODO: Handle distance properly.
            var aExtent = angle.ToVec() * ScaledMinimapRadius * 1.42f;
            var lineColor = Color.MediumSpringGreen.WithAlpha(0.02f);
            handle.DrawLine(origin - aExtent, origin + aExtent, lineColor);
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

    public virtual Angle GetMatrixRotation()
    {
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        if (!xformQuery.TryGetComponent(_coordinates!.Value.EntityId, out var xform))
            return Angle.Zero;

        return xform.WorldRotation + _rotation!.Value;
    }

    protected virtual Color GetBackground()
    {
        return Color.Black;
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
