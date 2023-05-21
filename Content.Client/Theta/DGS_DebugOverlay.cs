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
            DrawHatchedRect(freeRect, Color.Green, args.WorldHandle);
        }
        foreach(Box2i occupiedRect in OccupiedRects)
        {
            DrawHatchedRect(occupiedRect, Color.Yellow, args.WorldHandle);
        }
        for (int y = 0; y < 1000; y += 100)
        {
            for (int x = 0; x < 1000; x += 100)
            {
                args.WorldHandle.DrawRect(new Box2(new Vector2(x, y), new Vector2(x + 100, y + 100)), Color.Blue, false);
            }
        }
    }

    private void DrawHatchedRect(Box2i rect, Color color, DrawingHandleWorld handle)
    {
        handle.DrawRect(rect, color, false);
        for (int i = 0; i < rect.Width; i += 2)
        {
            handle.DrawLine(new Vector2i(rect.Left + i, rect.Bottom), new Vector2i(rect.Left + i, rect.Top), color);
        }
    }
}
