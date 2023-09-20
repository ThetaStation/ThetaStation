using Content.Server.Shuttles.Systems;
using Content.Server.Theta.RadarRenderable;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using SQLitePCL;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class CannonConsoleSystem : EntitySystem
{
    [Dependency] private readonly RadarConsoleSystem _radarConsoleSystem = default!;
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
    private void UpdateState(EntityUid uid, RadarConsoleComponent radarConsole, TransformComponent transform)
    {
        Angle? angle = Angle.Zero; // I fuck non north direction in the radar
        EntityCoordinates? coordinates = transform.Coordinates;

        var all = _radarRenderable.GetObjectsAround(uid, radarConsole);
        var cannonsInformation = _radarConsoleSystem.GetCannonInfosByMyGrid(uid, radarConsole);
        var doors = _radarConsoleSystem.GetDoorInfoByMyGrid(uid, radarConsole);
		var shield =_radarConsoleSystem.GetShieldsAround(radarConsole);

        var radarState = new CannonConsoleBoundInterfaceState(
            radarConsole.MaxRange,
            GetNetCoordinates(coordinates),
            angle,
            new List<DockingInterfaceState>(),
            cannonsInformation,
            doors,
            all,
            shield
        );

        _uiSystem.TrySetUiState(uid, CannonConsoleUiKey.Key, radarState);
    }
}
