using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Theta;

//REMOVE LATER
public sealed class DebugOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public List<Box2i> FreeRects = new();
    public List<Box2i> OccupiedRects = new();

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach(Box2i freeRect in FreeRects)
        {
            args.WorldHandle.DrawRect(freeRect, Color.Green, false);
        }
        foreach(Box2i occupiedRect in OccupiedRects)
        {
            args.WorldHandle.DrawLine(occupiedRect.BottomLeft, occupiedRect.TopRight, Color.Yellow);
            args.WorldHandle.DrawLine(occupiedRect.BottomRight, occupiedRect.TopLeft, Color.Yellow);
        }
        for (int y = 0; y < 1000; y += 100)
        {
            for (int x = 0; x < 1000; x += 100)
            {
                args.WorldHandle.DrawRect(new Box2(new Vector2(x, y), new Vector2(x + 100, y + 100)), Color.Orange, false);
            }
        }
    }
}
