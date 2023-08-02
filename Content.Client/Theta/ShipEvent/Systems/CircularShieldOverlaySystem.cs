using Robust.Client.Graphics;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class CircularShieldOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager overlayMan = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        overlayMan.AddOverlay(new CircularShieldOverlay(EntityManager));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        overlayMan.RemoveOverlay<CircularShieldOverlay>();
    }
}
