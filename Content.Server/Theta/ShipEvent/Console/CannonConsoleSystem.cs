using Content.Server.Theta.RadarRenderable;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.UI;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class CannonConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly RadarRenderableSystem _radarRenderable = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CannonConsoleComponent, RadarConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var radar, out var transform))
        {
            if (!_uiSystem.IsUiOpen(uid, CannonConsoleUiKey.Key))
                continue;
            UpdateState(uid, radar, transform);
        }
    }

    public List<CannonInformationInterfaceState> GetCannonInfoByMyGrid(EntityUid uid, RadarConsoleComponent console)
    {
        var list = new List<CannonInformationInterfaceState>();
        var myGrid = Transform(uid).GridUid;
        var cannonQuery = EntityQueryEnumerator<TransformComponent, CannonComponent>();

        while (cannonQuery.MoveNext(out var cannonUid, out var form, out var cannon))
        {
            if (form.GridUid != myGrid || !form.Anchored)
                continue;

            var (ammo, maxAmmo) = GetCannonAmmoCount(cannonUid, cannon);

            list.Add(new CannonInformationInterfaceState
            {
                Uid = GetNetEntity(cannonUid),
                IsControlling = cannon.BoundConsoleUid == uid,
                Ammo = ammo,
                MaxAmmo = maxAmmo
            });
        }

        return list;
    }

    public (int ammo, int maxAmmo) GetCannonAmmoCount(EntityUid cannonUid, CannonComponent? cannon)
    {
        if (!Resolve(cannonUid, ref cannon))
            return (0, 0);

        var ammoCountEv = new GetAmmoCountEvent();
        RaiseLocalEvent(cannonUid, ref ammoCountEv);

        return (ammoCountEv.Count, ammoCountEv.Capacity);
    }

    private void UpdateState(EntityUid uid, RadarConsoleComponent radarConsole, TransformComponent transform)
    {
        Angle? angle = Angle.Zero; // I fuck non north direction in the radar
        EntityCoordinates? coordinates = transform.Coordinates;

        var all = _radarRenderable.GetObjectsAround(uid, radarConsole);
        var cannonsInformation = GetCannonInfoByMyGrid(uid, radarConsole);

        var radarState = new CannonConsoleBoundInterfaceState(
            radarConsole.MaxRange,
            GetNetCoordinates(coordinates),
            angle,
            new List<DockingInterfaceState>(), //pzdc
            cannonsInformation,
            all
        );

        _uiSystem.TrySetUiState(uid, CannonConsoleUiKey.Key, radarState);
    }
}
