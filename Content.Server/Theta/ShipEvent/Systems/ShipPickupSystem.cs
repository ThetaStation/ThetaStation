using Content.Server.Explosion.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.ShipEvent.Components;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipPickupSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _rand = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShipPickupableComponent, TriggerEvent>(OnPickupTrigger);
    }

    private void OnPickupTrigger(EntityUid uid, ShipPickupableComponent pickup, TriggerEvent args)
    {
        if (args.User == null || !HasComp<ShuttleComponent>(args.User))
            return;

        foreach (var (beacon, transform) in EntityQuery<ShipPickupBeaconComponent, TransformComponent>())
        {
            if (transform.GridUid == args.User && pickup.TargetBeaconId == beacon.Id)
            {
                foreach(string entProtoId in pickup.EntsToSpawn)
                {
                    Spawn(entProtoId, transform.Coordinates);
                }
                break;
            }
        }
    }
}
