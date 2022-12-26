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
        SubscribeNetworkEvent<RotateCannonsEvent>(OnRotateCannons);
    }

    private void OnRotateCannons(RotateCannonsEvent ev)
    {
        foreach (var uid in ev.Cannons)
        {
            _rotateToFaceSystem.TryFaceCoordinates(uid, ev.Coordinates);
        }
    }
}
