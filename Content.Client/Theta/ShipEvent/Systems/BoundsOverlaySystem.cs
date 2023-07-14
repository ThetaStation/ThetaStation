using Content.Shared.Theta.ShipEvent;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class BoundsOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overMan = default!;
    public Box2 CurrentBounds;
    private BoundsOverlay overlay = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeAllEvent<BoundsOverlayInfo>(OnInfoReceived);
        overlay = new BoundsOverlay();
        _overMan.AddOverlay(overlay);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        RaiseNetworkEvent(new BoundsOverlayInfoRequest());
    }

    private void OnInfoReceived(BoundsOverlayInfo ev)
    {
        CurrentBounds = ev.Bounds;
        overlay.Bounds = ev.Bounds;
        overlay.TargetMap = ev.TargetMap;
    }
}
