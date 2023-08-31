using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Theta.ShipEvent;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Console;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Doors.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;

namespace Content.Server.Theta.RadarRenderable;

public sealed class RadarRenderableSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly RadarConsoleSystem _radarConsoleSystem = default!;

    public List<CommonRadarEntityInterfaceState> GetObjectsAround(EntityUid consoleUid, RadarConsoleComponent radar)
    {
        var states = new List<CommonRadarEntityInterfaceState>();
        if (!TryComp<TransformComponent>(consoleUid, out var xform))
            return states;
        states.AddRange(GetRadarRenderableStates(consoleUid, radar, xform));
        return states;
    }

    private List<CommonRadarEntityInterfaceState> GetRadarRenderableStates(EntityUid consoleUid,
        RadarConsoleComponent radar,
        TransformComponent xform)
    {
        var states = new List<CommonRadarEntityInterfaceState>();
        var query = EntityQueryEnumerator<RadarRenderableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var radarRenderable, out var transform))
        {
            if (!_radarConsoleSystem.HasFlag(radar, (RadarRenderableGroup) radarRenderable.Group))
                continue;
            if (!xform.MapPosition.InRange(transform.MapPosition, radar.MaxRange))
                continue;

            CommonRadarEntityInterfaceState? state;
            switch ((RadarRenderableGroup) radarRenderable.Group)
            {
                case RadarRenderableGroup.ShipEventTeammate:
                    state = GetMobState(uid, radarRenderable, transform);
                    break;
                case RadarRenderableGroup.Cannon:
                    state = GetCannonState(uid, consoleUid, radarRenderable, xform, transform);
                    break;
                case RadarRenderableGroup.Door:
                    state = GetDoorState(uid, radarRenderable, transform);
                    break;
                default:
                    state = GetDefaultState(uid, radarRenderable, transform);
                    break;
            }

            if (state != null)
                states.Add(state);
        }

        return states;
    }

    private CommonRadarEntityInterfaceState? GetCannonState(EntityUid uid, EntityUid consoleUid,
        RadarRenderableComponent radarRenderable, TransformComponent consoleTransform, TransformComponent xform)
    {
        if (!TryComp<CannonComponent>(uid, out var cannon))
            return null;

        var myGrid = consoleTransform.GridUid;
        var isCannonConsole = HasComp<CannonConsoleComponent>(consoleUid);

        var controlledCannons = _radarConsoleSystem.GetControlledCannons(consoleUid);
        if (Transform(uid).GridUid != myGrid)
            return null;
        if (!Transform(uid).Anchored)
            return null;

        var controlled = false;
        if (controlledCannons != null)
            controlled = controlledCannons.Contains(uid);

        var (usedCapacity, maxCapacity) = _radarConsoleSystem.GetCannonAmmoCount(uid, cannon);
        var mainColor = controlled ? Color.Lime : (isCannonConsole ? Color.LightGreen : Color.YellowGreen);

        var hsvColor = Color.ToHsv(mainColor);
        const float additionalDegreeCoeff = 20f / 360f;
        // X is hue
        var hueOffset = hsvColor.X * usedCapacity / Math.Max(1, maxCapacity);
        hsvColor.X = Math.Max(hueOffset + additionalDegreeCoeff, additionalDegreeCoeff);

        mainColor = Color.FromHsv(hsvColor);


        return new CommonRadarEntityInterfaceState(
            _transformSystem.GetMoverCoordinates(uid, xform),
            _transformSystem.GetWorldRotation(xform),
            radarRenderable.RadarView,
            mainColor
        );
    }

    private CommonRadarEntityInterfaceState? GetDefaultState(EntityUid uid, RadarRenderableComponent renderable,
        TransformComponent xform)
    {
        return new CommonRadarEntityInterfaceState(
            _transformSystem.GetMoverCoordinates(uid, xform),
            _transformSystem.GetWorldRotation(xform),
            renderable.RadarView
        );
    }

    private CommonRadarEntityInterfaceState? GetMobState(EntityUid uid, RadarRenderableComponent renderable,
        TransformComponent xform)
    {
        if (!TryComp<MindContainerComponent>(uid, out var mindContainer) ||
            !TryComp<MobStateComponent>(uid, out var mobState))
            return null;
        if (_mobStateSystem.IsIncapacitated(uid, mobState))
            return null;
        Color? color = null;
        if (mindContainer.Mind != null)
        {
            foreach (var role in mindContainer.Mind.AllRoles)
            {
                if (role is ShipEventRole && role.Faction is ShipEventFaction shipEventFaction)
                    color = shipEventFaction.Color;
            }
        }

        return new CommonRadarEntityInterfaceState(
            _transformSystem.GetMoverCoordinates(uid, xform),
            _transformSystem.GetWorldRotation(xform),
            renderable.RadarView,
            color
        );
    }

    private CommonRadarEntityInterfaceState? GetDoorState(EntityUid uid, RadarRenderableComponent renderable, TransformComponent xform)
    {
        if (!TryComp<DoorComponent>(uid, out var door))
            return null;

        Color? color = Color.White;

        if (door.State == DoorState.Closed)
        {
            color = Color.Red;
        }
        else if (door.State == DoorState.Opening | door.State == DoorState.Closing)
        {
            color = Color.Yellow;
        }
        else
        {
            color = Color.LimeGreen;
        }

        return new CommonRadarEntityInterfaceState(
            _transformSystem.GetMoverCoordinates(uid, xform),
            _transformSystem.GetWorldRotation(xform),
            renderable.RadarView,
            color
        );
    }
}
