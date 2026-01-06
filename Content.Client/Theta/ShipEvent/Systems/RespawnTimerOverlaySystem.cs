using Content.Shared.Theta.ShipEvent;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class RespawnTimerOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overMan = default!;
    private RespawnTimerOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RespawnTimerOverlayInfo>(OnInfoReceived);
        _overlay = new();
        _overMan.AddOverlay(_overlay);
    }

    private void OnInfoReceived(RespawnTimerOverlayInfo ev)
    {
        _overlay.TimeCountdown = TimeSpan.FromSeconds(ev.Time + 1);
    }
}
