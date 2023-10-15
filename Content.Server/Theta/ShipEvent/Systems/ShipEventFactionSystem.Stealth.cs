using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Timing;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventFactionSystem
{
    [Dependency] private ShuttleSystem _iffSys = default!;

    private HashSet<EntityUid> OnCooldown = new ();

    private void InitializeStealth()
    {
        SubscribeLocalEvent<ShuttleConsoleComponent, ShipEventToggleStealthMessage>(OnStealthActivated);
        SubscribeLocalEvent<ShuttleConsoleComponent, ShipEventRequestStealthStatusMessage>(OnStealthStatusRequest);
    }

    public void OnStealthActivated(EntityUid uid, ShuttleConsoleComponent _, ShipEventToggleStealthMessage args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform) || xform.GridUid == null)
            return;

        var shuttle = xform.GridUid.Value;
        if (OnCooldown.Contains(shuttle)) return;
        
        if (!TryComp<ShipStealthComponent>(uid, out var stealth))
            return;

        //multiplying by thousand since component fields are in seconds
        _iffSys.AddIFFFlag(shuttle, IFFFlags.Hide);
        Timer.Spawn(stealth.StealthDuration * 1000, () => { _iffSys.RemoveIFFFlag(shuttle, IFFFlags.Hide); });

        OnCooldown.Add(shuttle);
        Timer.Spawn((stealth.StealthDuration + stealth.StealthCooldown) * 1000, () =>
        {
            OnCooldown.Remove(shuttle);
            RaiseNetworkEvent(new ShipEventStealthStatusMessage(true, EntityManager.GetNetEntity(uid)), args.Session);
        });
    }
    
    private void OnStealthStatusRequest(EntityUid uid, ShuttleConsoleComponent _, ShipEventRequestStealthStatusMessage args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform) || xform.GridUid == null)
            return;

        var shuttle = xform.GridUid.Value;
        RaiseNetworkEvent(new ShipEventStealthStatusMessage(!OnCooldown.Contains(shuttle), EntityManager.GetNetEntity(uid)), args.Session);
        
        //todo: no idea why this doesn't work
        /*if (_uiSys.TryGetUi(uid, args.UiKey, out var bui))
            _uiSys.SetUiState(bui, new ShipEventStealthStatusMessage(!OnCooldown.Contains(shuttle)), args.Session);*/
    }
}
