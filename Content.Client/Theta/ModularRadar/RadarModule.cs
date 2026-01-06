using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.Theta.ModularRadar;

public abstract class RadarModule
{
    [Dependency] protected readonly IEntityManager EntManager = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;

    protected ModularRadarControl Radar;

    protected EntityCoordinates? ParentCoordinates => Radar.GetConsoleCoordinates();
    protected Angle? ParentRotation => Radar.GetConsoleRotation();
    protected EntityUid ParentUid => Radar.OwnerUid;

    protected Vector2 MaxRadarRangeVector => new Vector2(MaxRadarRange, MaxRadarRange);

    protected Vector2 MidpointVector => new Vector2(MidPoint, MidPoint);
    protected int MidPoint => SizeFull / 2;
    protected int SizeFull => (int) ((Radar.GetUIDisplayRadius() + Radar.GetMinimapMargin()) * 2 * Radar.UIScale);
    protected int ScaledMinimapRadius => (int) (Radar.GetUIDisplayRadius() * Radar.UIScale);
    protected float MinimapScale => Radar.WorldRange != 0 ? ScaledMinimapRadius / Radar.WorldRange : 0f;
    protected float NormalMinimapScale => MinimapScale / (ScaledMinimapRadius / 64);
    protected float MaxRadarRange => Radar.MaxRadarRange;
    protected float WorldRange => Radar.WorldRange;
    protected float ActualRadarRange => Radar.GetActualRadarRange();
    protected Matrix3x2 OffsetMatrix => Radar.GetInvertOffsetMatrix();
    protected int PixelHeight => Radar.PixelHeight;
    protected int PixelWidth => Radar.PixelWidth;

    protected RadarModule(ModularRadarControl parentRadar)
    {
        IoCManager.InjectDependencies(this);
        Radar = parentRadar;
    }

    public virtual void MouseMove(GUIMouseMoveEventArgs args) { }
    public virtual void OnKeyBindDown(GUIBoundKeyEventArgs args) { }
    public virtual void OnKeyBindUp(GUIBoundKeyEventArgs args) { }
    public virtual void UpdateState(BoundUserInterfaceState state) { }
    public virtual void Draw(DrawingHandleScreen handle, Parameters parameters) { }
    public virtual void FrameUpdate(FrameEventArgs args) { }
    public virtual void OnClear() { }

    protected Vector2 RelativeToWorld(Vector2 pos, Matrix3x2 matrix)
    {
        pos = InverseScalePosition(pos);
        pos.Y = -pos.Y;
        return Vector2.Transform(pos, matrix);
    }

    protected Vector2 WorldToRelative(Vector2 pos, Matrix3x2 matrix)
    {
        pos = Vector2.Transform(pos, matrix);
        pos.Y = -pos.Y;
        return ScalePosition(pos);
    }

    protected Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidpointVector;
    }

    protected Vector2 InverseScalePosition(Vector2 value)
    {
        return (value - MidpointVector) / MinimapScale;
    }

    protected static Vector2 ScalePosition(Vector2 value, float minimapScale, Vector2 midpointVector)
    {
        return value * minimapScale + midpointVector;
    }

    public sealed class Parameters
    {
        public Matrix3x2 DrawMatrix;

        public Parameters(Matrix3x2 drawMatrix)
        {
            DrawMatrix = drawMatrix;
        }
    }
}
