using Content.Server.Explosion.EntitySystems;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.ShipEvent.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipPickupSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly SharedAudioSystem _audioSys = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShipPickupableComponent, TriggerEvent>(OnPickupTrigger);
    }

    private void OnPickupTrigger(EntityUid uid, ShipPickupableComponent pickup, TriggerEvent args)
    {
        if (args.User == null || !HasComp<ShuttleComponent>(args.User))
            return;

        var beaconQuery = EntityQueryEnumerator<ShipPickupBeaconComponent, TransformComponent>();
        while (beaconQuery.MoveNext(out var beaconUid, out var beacon, out var form))
        {
            if (form.GridUid == args.User && pickup.TargetBeaconId == beacon.Id)
            {
                foreach (string entProtoId in pickup.EntsToSpawn)
                {
                    Spawn(entProtoId, form.Coordinates);
                }

                _audioSys.PlayPredicted(beacon.TeleportationSound, beaconUid, beaconUid);

                break;
            }
        }
    }
}
