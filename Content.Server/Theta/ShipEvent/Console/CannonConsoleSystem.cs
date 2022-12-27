using System.Linq;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;
using Content.Server.Shuttles.Systems;
using Content.Shared.MachineLinking.Events;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Console;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class CannonConsoleSystem : EntitySystem
{
    [Dependency] private readonly RadarConsoleSystem _radarConsoleSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SignalLinkerSystem _signalSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CannonConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<CannonConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
    }

    private void OnNewLink(EntityUid uid, CannonConsoleComponent component, NewLinkEvent args)
    {
        if (!TryComp<CannonComponent>(args.Receiver, out var analyzer))
            return;

        component.LinkedCannons.Add(analyzer);
    }

    private void OnPortDisconnected(EntityUid uid, CannonConsoleComponent component, PortDisconnectedEvent args)
    {
        if (args.Port != component.LinkingPort)
            return;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var (cannon, radar) in EntityManager.EntityQuery<CannonConsoleComponent, RadarConsoleComponent>())
        {
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
        var cannonsOnGrid = _radarConsoleSystem.GetCannonsOnGrid(radarConsole);
        var controlledCannons = cannonConsole.LinkedCannons.Select(cannon => cannon.Owner).ToList();

        var radarState = new CannonConsoleBoundInterfaceState(
            radarConsole.MaxRange,
            coordinates,
            angle,
            new List<DockingInterfaceState>(),
            mobs,
            projectiles,
            cannonsOnGrid,
             controlledCannons
        );

        _uiSystem.TrySetUiState(cannonConsole.Owner, CannonConsoleUiKey.Key, radarState);
    }
}
