using Content.Server.Shuttles.Systems;
using Content.Server.Theta.RadarRenderable;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class CannonConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly RadarRenderableSystem _radarRenderable = default!;
    [Dependency] private readonly ShuttleConsoleSystem _shuttleConsole = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverrideSys = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CannonConsoleComponent, CannonConsoleBUICreatedMessage>(OnBUICreated);
        SubscribeLocalEvent<CannonConsoleComponent, CannonConsoleBUIDisposedMessage>(OnBUIDisposed);
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

    private void UpdateState(EntityUid uid, RadarConsoleComponent radarConsole, TransformComponent transform)
    {
        Angle? angle = Angle.Zero; // I fuck non north direction in the radar
        EntityCoordinates? coordinates = transform.Coordinates;

        var radarState = new RadarConsoleBoundInterfaceState(
            _shuttleConsole.GetNavState(uid, new Dictionary<NetEntity, List<DockingPortState>>()),
            new DockingInterfaceState(), //pzdc
            _radarRenderable.GetObjectsAround(uid, radarConsole)
        );

        _uiSystem.SetUiState(uid, CannonConsoleUiKey.Key, radarState);
    }

    private List<EntityUid> GetControlledCannons(EntityUid uid)
    {
        List<EntityUid> result = new();
        var query = EntityManager.EntityQueryEnumerator<CannonComponent>();
        while (query.MoveNext(out var cannonUid, out var cannon))
        {
            if (cannon.BoundConsoleUid == uid)
                result.Add(cannonUid);
        }

        return result;
    }

    private void OnBUICreated(EntityUid uid, CannonConsoleComponent console, CannonConsoleBUICreatedMessage msg)
    {
        if(!_playerManager.TryGetSessionByEntity(msg.Actor, out var session))
            return;
        foreach (EntityUid controlledUid in GetControlledCannons(uid))
        {
            _pvsOverrideSys.AddSessionOverride(controlledUid, session);
        }
    }

    private void OnBUIDisposed(EntityUid uid, CannonConsoleComponent console, CannonConsoleBUIDisposedMessage msg)
    {
        if(!_playerManager.TryGetSessionByEntity(msg.Actor, out var session))
            return;
        foreach (EntityUid controlledUid in GetControlledCannons(uid))
        {
            _pvsOverrideSys.RemoveSessionOverride(controlledUid, session);
        }
    }
}
