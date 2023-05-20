using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Theta;

public sealed class DebugOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        for (int y = 0; y < 1000; y += 100)
        {
            for (int x = 0; x < 1000; x += 100)
            {
                args.WorldHandle.DrawRect(new Box2(new Vector2(x, y), new Vector2(x + 100, y + 100)), Color.Green, filled:false);
            }
        }
    }
}
