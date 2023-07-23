using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Map;

namespace Content.Client.Theta.ModularRadar;

public abstract class RadarModule
{
    [Dependency] protected readonly IEntityManager EntManager = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;

    protected ModularRadarControl Radar;

    protected EntityCoordinates? ParentCoordinates => Radar.GetConsoleCoordinates();
    protected Angle? ParentRotation => Radar.GetConsoleRotation();
    protected EntityUid OwnerUid => Radar.OwnerUid;

    protected Vector2 MaxRadarRangeVector => new Vector2(MaxRadarRange, MaxRadarRange);

    protected Vector2 MidpointVector => new Vector2(MidPoint, MidPoint);
    protected int MidPoint => SizeFull / 2;
    protected int SizeFull => (int) ((Radar.GetUIDisplayRadius() + Radar.GetMinimapMargin()) * 2 * Radar.UIScale);
    protected int ScaledMinimapRadius => (int) (Radar.GetUIDisplayRadius() * Radar.UIScale);
    protected float MinimapScale => Radar.WorldRange != 0 ? ScaledMinimapRadius / Radar.WorldRange : 0f;
    protected float MaxRadarRange => Radar.MaxRadarRange;
    protected float WorldRange => Radar.WorldRange;
    protected float ActualRadarRange => Radar.GetActualRadarRange();
    protected Matrix3 GetOffsetMatrix => Radar.GetOffsetMatrix().Invert();

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
    public virtual void OnClear() { }

    protected Vector2 RelativePositionToCoordinates(Vector2 pos, Matrix3 matrix)
    {
        var removeScale = InverseScalePosition(pos);
        removeScale.Y = -removeScale.Y;
        return matrix.Transform(removeScale);
    }

    protected Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidpointVector;
    }

    protected Vector2 InverseScalePosition(Vector2 value)
    {
        return (value - MidpointVector) / MinimapScale;
    }

    public sealed class Parameters
    {
        public Matrix3 DrawMatrix;

        public Parameters(Matrix3 drawMatrix)
        {
            DrawMatrix = drawMatrix;
        }
    }
}
