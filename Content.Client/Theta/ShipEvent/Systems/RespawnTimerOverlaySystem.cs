using Content.Shared.Theta.ShipEvent;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class RespawnTimerOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overMan = default!;
    public Box2 CurrentBounds;
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
        _overlay.Time = TimeSpan.FromSeconds(ev.Time);
    }
}