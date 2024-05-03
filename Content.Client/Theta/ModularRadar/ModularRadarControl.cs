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

    private const int CircleDistance = 50; //in world meters

    /// <summary>
    /// Used to transform all of the radar objects. Typically is a shuttle console parented to a grid.
    /// </summary>
    private EntityCoordinates? _coordinates;
    private Angle? _rotation;
    public EntityUid OwnerUid;

    protected readonly List<RadarModule> Modules = new();

    public Action? OnParentUidSet;

    private Font _font;

    public ModularRadarControl(float minRange = 64f, float maxRange = 256f, float range = 256f)
        : base(minRange, maxRange, range)
    {
        _font = IoCManager.Resolve<IResourceCache>().GetFont("/Fonts/NotoSans/NotoSans-Regular.ttf", 6);
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
                UpdateMaxRange(state.MaxRange);
                break;
            case ShieldConsoleBoundsUserInterfaceState shieldState:
                UpdateMaxRange(shieldState.MaxRange);
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

        var background = GetBackground();
        handle.DrawRect(new UIBox2(Vector2.Zero, new Vector2(SizeFull)), background);

        // No data
        if (_coordinates == null || _rotation == null)
        {
            Clear();
            return;
        }

        var linesRadial = 8;
        for (var i = 0; i < linesRadial; i++)
        {
            Angle angle = Math.PI / linesRadial * i;
            Vector2 extent = angle.ToVec() * SizeFull;
            handle.DrawLine(MidpointVector - extent, MidpointVector + extent, new Color(0.08f, 0.08f, 0.08f));
        }

        var circles = (int) WorldRange / CircleDistance;
        for (var i = 0; i < circles; i++)
        {
            var radius = (i + 1) * CircleDistance;
            handle.DrawString(_font, new Vector2(MidPoint, MidPoint - radius * MinimapScale), radius.ToString(), Color.DimGray);
            handle.DrawCircle(MidpointVector, radius * MinimapScale, Color.DimGray, false);
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
