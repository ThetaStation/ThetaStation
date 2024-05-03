using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventFactionSystem
{
    [Dependency] private ShuttleSystem _iffSys = default!;

    private HashSet<EntityUid> _onCooldown = new();

    private void InitializeStealth()
    {
        SubscribeLocalEvent<ShipStealthComponent, ComponentInit>(OnStealthInit);
        SubscribeLocalEvent<ShuttleConsoleComponent, ShipEventToggleStealthMessage>(OnStealthActivated);
        SubscribeLocalEvent<ShuttleConsoleComponent, ShipEventRequestStealthStatusMessage>(OnStealthStatusRequest);
    }

    private void OnStealthInit(EntityUid uid, ShipStealthComponent stealth, ComponentInit args)
    {
        TryActivateStealth(uid, 10); //spawn camping measure
    }

    private void OnStealthActivated(EntityUid uid, ShuttleConsoleComponent _, ShipEventToggleStealthMessage args)
    {
        TryActivateStealth(uid);
    }

    private bool TryActivateStealth(EntityUid uid, int? durationOverride = null, int? cdOverride = null)
    {
        var xform = Transform(uid);
        if (xform.GridUid == null)
            return false;

        var shuttle = xform.GridUid.Value;
        if (_onCooldown.Contains(shuttle))
            return false;

        if (!TryComp<ShipStealthComponent>(uid, out var stealth))
            return false;

        //multiplying by thousand since component fields are in seconds
        int duration = (durationOverride ?? stealth.StealthDuration) * 1000;
        int cooldown = (cdOverride ?? stealth.StealthCooldown) * 1000;

        _iffSys.AddIFFFlag(shuttle, IFFFlags.Hide);
        Timer.Spawn(duration, () => { _iffSys.RemoveIFFFlag(shuttle, IFFFlags.Hide); });

        _onCooldown.Add(shuttle);
        Timer.Spawn(duration + cooldown, () =>
        {
            _onCooldown.Remove(shuttle);
            if (EntityManager.EntityExists(uid))
                RaiseNetworkEvent(new ShipEventStealthStatusMessage(true, EntityManager.GetNetEntity(uid)));
        });

        return true;
    }

    private void OnStealthStatusRequest(EntityUid uid, ShuttleConsoleComponent _, ShipEventRequestStealthStatusMessage args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform) || xform.GridUid == null)
            return;

        var shuttle = xform.GridUid.Value;
        RaiseNetworkEvent(new ShipEventStealthStatusMessage(!_onCooldown.Contains(shuttle), EntityManager.GetNetEntity(uid)), args.Session);

        //todo: no idea why this doesn't work
        /*if (_uiSys.TryGetUi(uid, args.UiKey, out var bui))
            _uiSys.SetUiState(bui, new ShipEventStealthStatusMessage(!OnCooldown.Contains(shuttle)), args.Session);*/
    }
}
