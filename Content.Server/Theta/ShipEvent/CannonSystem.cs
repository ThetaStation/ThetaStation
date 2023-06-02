using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Interaction;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.UI;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server.Theta.ShipEvent;

public sealed class CannonSystem : SharedCannonSystem
{
    [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RotateCannonsEvent>(OnRotateCannons);
        SubscribeLocalEvent<CannonComponent, AmmoShotEvent>(AfterShot);
        SubscribeLocalEvent<CannonComponent, ComponentRemove>(OnRemoval);
    }

    private void OnRemoval(EntityUid uid, CannonComponent cannon, ComponentRemove args)
    {
        if(cannon.BoundLoader != null)
            cannon.BoundLoader.BoundTurret = null;
    }

    private void OnRotateCannons(RotateCannonsEvent ev)
    {
        foreach (var uid in ev.Cannons)
        {
            _rotateToFaceSystem.TryFaceCoordinates(uid, ev.Coordinates);
        }
    }

    private void AfterShot(EntityUid entity, CannonComponent cannon, AmmoShotEvent args)
    {
        foreach(EntityUid projectile in args.FiredProjectiles)
        {
            var marker = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(projectile);
            marker.Team = EntityManager.EnsureComponent<ShipEventFactionMarkerComponent>(entity).Team;
        }

        if (cannon.BoundLoaderEntity != null)
            RaiseLocalEvent(cannon.BoundLoaderEntity.Value, new TurretLoaderAfterShotMessage());
    }
}
