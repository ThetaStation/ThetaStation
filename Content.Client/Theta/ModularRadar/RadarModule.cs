using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Map;

namespace Content.Client.Theta.ModularRadar;

public abstract class RadarModule
{
    [Dependency] protected readonly IEntityManager EntManager = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;

    protected EntityCoordinates? ParentCoordinates;

    protected Angle? ParentRotation;

    protected RadarModule()
    {
        IoCManager.InjectDependencies(this);
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        ParentCoordinates = coordinates;
        ParentRotation = angle;
    }

    public virtual void MouseMove(GUIMouseMoveEventArgs args) { }
    public virtual void OnKeyBindDown(GUIBoundKeyEventArgs args) { }
    public virtual void OnKeyBindUp(GUIBoundKeyEventArgs args) { }
    public virtual void UpdateState(BoundUserInterfaceState state) { }
    public virtual void Draw(DrawingHandleScreen handle, Parameters parameters) { }
    public virtual void OnClear() { }

    protected static Vector2 ScalePosition(Vector2 value, Parameters parameters)
    {
        return value * parameters.MinimapScale + parameters.MidPoint;
    }

    protected static Vector2 InverseScalePosition(Vector2 value, Parameters parameters)
    {
        return (value - parameters.MidPoint) / parameters.MinimapScale;
    }

    public sealed class Parameters
    {
        public int MidPoint;
        public float MinimapScale;
        public Matrix3 Matrix;
        public float MaxRadarRange;
        public float ActualRadarRange;
        public ModularRadarControl RadarControl;
        public float WorldRange;

        public Parameters(int midPoint,
            float minimapScale,
            Matrix3 matrix,
            float maxRadarRange,
            float actualRadarRange,
            ModularRadarControl radarControl,
            float worldRange)
        {
            MidPoint = midPoint;
            MinimapScale = minimapScale;
            Matrix = matrix;
            MaxRadarRange = maxRadarRange;
            ActualRadarRange = actualRadarRange;
            RadarControl = radarControl;
            WorldRange = worldRange;
        }
    }
}
