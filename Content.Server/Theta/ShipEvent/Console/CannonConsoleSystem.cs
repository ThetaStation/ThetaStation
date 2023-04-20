using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class CannonConsoleSystem : EntitySystem
{
    [Dependency] private readonly RadarConsoleSystem _radarConsoleSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (cannon, radar) in EntityManager.EntityQuery<CannonConsoleComponent, RadarConsoleComponent>())
        {
            if (!_uiSystem.IsUiOpen(cannon.Owner, CannonConsoleUiKey.Key))
                continue;
            UpdateState(cannon, radar);
        }
    }

    private void UpdateState(CannonConsoleComponent cannonConsole, RadarConsoleComponent radarConsole)
    {
        var xform = Transform(cannonConsole.Owner);
        Angle? angle = Angle.Zero; // I fuck non north direction in the radar
        EntityCoordinates? coordinates = xform.Coordinates;

        var mobs = _radarConsoleSystem.GetMobsAround(radarConsole);
        var projectiles = _radarConsoleSystem.GetProjectilesAround(radarConsole);
        var cannonsInformation = _radarConsoleSystem.GetCannonInfosByMyGrid(radarConsole);

        var radarState = new CannonConsoleBoundInterfaceState(
            radarConsole.MaxRange,
            coordinates,
            angle,
            new List<DockingInterfaceState>(),
            mobs,
            projectiles,
            cannonsInformation
        );

        _uiSystem.TrySetUiState(cannonConsole.Owner, CannonConsoleUiKey.Key, radarState);
    }
}
