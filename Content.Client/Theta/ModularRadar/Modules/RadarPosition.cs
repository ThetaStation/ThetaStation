using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarPosition : RadarModule
{
    public RadarPosition(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        var offset = ParentCoordinates!.Value.Position;
        var invertedPosition = ParentCoordinates.Value.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;

        handle.DrawCircle(ScalePosition(invertedPosition), 5f, Color.Lime);
    }
}
