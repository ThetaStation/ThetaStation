using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarPosition : RadarModule
{
    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        var offset = ParentCoordinates!.Value.Position;
        var invertedPosition = ParentCoordinates.Value.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;

        handle.DrawCircle(ScalePosition(invertedPosition, parameters), 5f, Color.Lime);
    }
}
