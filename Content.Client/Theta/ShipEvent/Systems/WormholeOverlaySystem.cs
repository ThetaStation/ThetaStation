using System.Numerics;
using Content.Shared.Theta.ShipEvent;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class WormholeOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    private WormholeOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new WormholeOverlay();
        _overlayMan.AddOverlay(_overlay);
        SubscribeNetworkEvent<WormholeOverlayAddGrid>(AddGrid);
        SubscribeNetworkEvent<WormholeOverlayRemoveGrid>(RemoveGrid);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<WormholeOverlay>();
    }

    private void AddGrid(WormholeOverlayAddGrid args)
    {
        WormholeOverlayGridParameters gridParams = new()
        {
            Reverse = args.Reverse,
            AttractionCenter = args.AttractionCenter,
            StartupTime = TimeSpan.Zero,
            Duration = 0
        };
        _overlay.Grids[GetEntity(args.GridUid)] = gridParams;
    }

    private void RemoveGrid(WormholeOverlayRemoveGrid args)
    {
        _overlay.Grids.Remove(GetEntity(args.GridUid));
    }
}

public sealed class WormholeOverlayGridParameters
{
    public bool Reverse;
    public Vector2 AttractionCenter;
    public TimeSpan StartupTime;
    public float Duration;
}
