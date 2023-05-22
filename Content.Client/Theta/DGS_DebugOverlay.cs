using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Content.Client.Theta;

//REMOVE LATER
public sealed class DebugOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public List<Box2i> FreeRects = new();
    public List<Vector2i> SpawnPositions = new();

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach(Box2i freeRect in FreeRects)
        {
            DrawCrossedRect(freeRect, Color.Green, args.WorldHandle);
        }
        foreach (Vector2i spawnPos in SpawnPositions)
        {
            args.WorldHandle.DrawRect(new Box2(
                new Vector2(spawnPos.X - 0.5f, spawnPos.Y - 0.5f), new Vector2(spawnPos.X + 0.5f, spawnPos.Y + 0.5f)), 
                Color.Red);
        }
        for (int y = 0; y < 1000; y += 100)
        {
            for (int x = 0; x < 1000; x += 100)
            {
                args.WorldHandle.DrawRect(new Box2(new Vector2(x, y), new Vector2(x + 100, y + 100)), Color.Blue, false);
            }
        }
    }

    private void DrawCrossedRect(Box2i rect, Color color, DrawingHandleWorld handle)
    {
        handle.DrawRect(rect, color, false);
        handle.DrawLine(rect.BottomLeft, rect.TopRight, color);
        handle.DrawLine(rect.TopLeft, rect.BottomRight, color);
    }
}
