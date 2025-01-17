using System.Numerics;
using Content.Server.Shuttles.Systems;
using Content.Server.Theta.RadarRenderable;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class RocketConsoleSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _formSys = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly RadarRenderableSystem _radarSys = default!;
    [Dependency] private readonly ShuttleConsoleSystem _shuttleConSys = default!;
    [Dependency] private readonly GunSystem _gunSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RocketConsoleComponent, RocketConsoleLaunchMessage>(OnLaunch);
        SubscribeLocalEvent<RocketLauncherComponent, AmmoShotEvent>(AfterShot);
        SubscribeLocalEvent<RocketLauncherComponent, NewLinkEvent>(OnLink);
        SubscribeLocalEvent<RocketLauncherComponent, SinkSourceSetEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, RocketLauncherComponent launcher, SinkSourceSetEvent args)
    {
        if (EntityManager.TryGetComponent<DeviceLinkSinkComponent>(uid, out var sink))
        {
            foreach (EntityUid sourceUid in sink.LinkedSources)
            {
                if (TryComp<RocketConsoleComponent>(sourceUid, out var console))
                    console.BoundLauncher = uid;
            }
        }
    }

    private void OnLink(EntityUid uid, RocketLauncherComponent launcher, ref NewLinkEvent args)
    {
        if (args.Sink != uid) //console is the source
            return;

        if (TryComp<RocketConsoleComponent>(args.Source, out var console))
            console.BoundLauncher = uid;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RocketConsoleComponent, RadarConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var radar, out var transform))
        {
            if (!_uiSys.IsUiOpen(uid, RocketConsoleUiKey.Key))
                continue;

            UpdateState(uid, radar, transform);
        }
    }

    private void UpdateState(EntityUid uid, RadarConsoleComponent radarConsole, TransformComponent transform)
    {
        Angle? angle = Angle.Zero;
        EntityCoordinates? coordinates = transform.Coordinates;

        var radarState = new RadarConsoleBoundInterfaceState(
            _shuttleConSys.GetNavState(uid, new Dictionary<NetEntity, List<DockingPortState>>()),
            new DockingInterfaceState(),
            _radarSys.GetObjectsAround(uid, radarConsole)
        );

        _uiSys.SetUiState(uid, RocketConsoleUiKey.Key, radarState);
    }

    private void OnLaunch(EntityUid uid, RocketConsoleComponent console, ref RocketConsoleLaunchMessage args)
    {
        if (args.Waypoints.Count == 0)
            return;

        if (TryComp<RocketLauncherComponent>(console.BoundLauncher, out var launcher) &&
            TryComp<GunComponent>(console.BoundLauncher, out var gun))
        {
            EntityUid? mapUid = Transform(console.BoundLauncher.Value).MapUid;
            if (mapUid == null)
                return;

            launcher.Waypoints = args.Waypoints;
            _gunSys.AttemptShoot(console.BoundLauncher.Value, console.BoundLauncher.Value, gun, new EntityCoordinates(mapUid.Value, args.Waypoints[0]));
        }
    }

    private void AfterShot(EntityUid uid, RocketLauncherComponent launcher, ref AmmoShotEvent args)
    {
        if (launcher.Waypoints == null)
            return;

        foreach (EntityUid rocketUid in args.FiredProjectiles)
        {
            if (TryComp<GuidedProjectileComponent>(rocketUid, out var proj))
            {
                proj.Waypoints = new List<Vector2> { _formSys.GetWorldPosition(uid) };
                proj.Waypoints.AddRange(launcher.Waypoints);
            }
        }

        launcher.Waypoints = null;
    }
}