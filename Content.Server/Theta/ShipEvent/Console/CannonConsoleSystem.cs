using Content.Server.Theta.RadarRenderable;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Map;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class CannonConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly RadarRenderableSystem _radarRenderable = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverrideSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CannonConsoleComponent, CannonConsoleBUIStateMessage>(OnBUIStateChange);
    }

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

    public void OnBUIStateChange(EntityUid uid, CannonConsoleComponent console, CannonConsoleBUIStateMessage msg)
    {
        List<EntityUid> controlledUids = new();
        var query = EntityManager.EntityQueryEnumerator<CannonComponent>();
        while (query.MoveNext(out var cannonUid, out var cannon))
        {
            if (cannon.BoundConsoleUid == uid)
                controlledUids.Add(cannonUid);
        }

        foreach (EntityUid controlledUid in controlledUids)
        {
            if (msg.Created)
            {
                _pvsOverrideSys.AddSessionOverride(controlledUid, msg.Session);
            }
            else
            {
                _pvsOverrideSys.RemoveSessionOverride(controlledUid, msg.Session);
            }
        }
    }

    private void UpdateState(EntityUid uid, RadarConsoleComponent radarConsole, TransformComponent transform)
    {
        Angle? angle = Angle.Zero; // I fuck non north direction in the radar
        EntityCoordinates? coordinates = transform.Coordinates;

        var radarState = new RadarConsoleBoundInterfaceState(
            radarConsole.MaxRange,
            GetNetCoordinates(coordinates),
            angle,
            new List<DockingInterfaceState>(), //pzdc
            _radarRenderable.GetObjectsAround(uid, radarConsole)
        );

        _uiSystem.TrySetUiState(uid, CannonConsoleUiKey.Key, radarState);
    }
}
