using Robust.Client.Graphics;

namespace Content.Client.Theta;

public sealed class DebugOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager overlayMan = default!;
    
    public override void Initialize()
    {
        overlayMan.AddOverlay(new DebugOverlay());
    }
}
