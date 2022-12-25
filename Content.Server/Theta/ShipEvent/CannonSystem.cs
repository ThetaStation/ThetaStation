using Content.Server.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Theta.ShipEvent;

namespace Content.Server.Theta.ShipEvent;

public sealed class CannonSystem : SharedCannonSystem
{
    [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CannonComponent, ComponentStartup>(OnCannonStartup);
        SubscribeNetworkEvent<RotateCannonsEvent>(OnRotateCannons);
    }

    private void OnRotateCannons(RotateCannonsEvent ev)
    {
        foreach (var uid in ev.Cannons)
        {
            _rotateToFaceSystem.TryFaceCoordinates(uid, ev.Coordinates);
        }
    }

    private void OnCannonStartup(EntityUid uid, CannonComponent component, ComponentStartup args)
    {
        // gun component required true
        if (TryComp<CombatModeComponent>(uid, out var combatMode))
        {
            combatMode.IsInCombatMode = true;
        }
    }
}
