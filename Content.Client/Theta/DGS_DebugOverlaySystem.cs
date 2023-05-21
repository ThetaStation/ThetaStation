using Content.Shared.Theta;
using Robust.Client.Graphics;

namespace Content.Client.Theta;

//REMOVE LATER
public sealed class DebugOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager overlayMan = default!;
    private DebugOverlay overlay = default!;

    public override void Initialize()
    {
        overlay = new DebugOverlay();
        overlayMan.AddOverlay(overlay);
        SubscribeNetworkEvent<FinalGridStateEvent>(OnFinalGridStateMessage);
    }

    public void OnFinalGridStateMessage(FinalGridStateEvent ev)
    {
        overlay.FreeRects = ev.FreeRangeRects;
        overlay.OccupiedRects = ev.OccupiedRangeRects;
    }
}
