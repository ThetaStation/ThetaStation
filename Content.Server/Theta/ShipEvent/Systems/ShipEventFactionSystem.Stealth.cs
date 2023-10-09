using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Timing;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventFactionSystem
{
    [Dependency] private ShuttleSystem _iffSys = default!;

    private HashSet<EntityUid> OnCooldown = new ();
    public int StealthDuration;
    public int StealthCooldown;

    private void InitializeStealth()
    {
        SubscribeLocalEvent<IFFConsoleComponent, ShipEventToggleStealthMessage>(OnStealthActivated);
    }

    public void OnStealthActivated(EntityUid uid, IFFConsoleComponent component, ShipEventToggleStealthMessage args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform) || xform.GridUid == null ||
            (component.AllowedFlags & IFFFlags.HideLabel) == 0x0)
            return;

        var shuttle = xform.GridUid.Value;

        if (OnCooldown.Contains(shuttle)) return;

        _iffSys.AddIFFFlag(shuttle, IFFFlags.Hide);
        Timer.Spawn(StealthDuration, () => { _iffSys.RemoveIFFFlag(shuttle, IFFFlags.Hide); });

        OnCooldown.Add(shuttle);
        Timer.Spawn(StealthDuration + StealthCooldown, () => { OnCooldown.Remove(shuttle); });
    }
}
