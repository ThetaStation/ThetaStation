using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Theta.ShipEvent;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Console;
using Content.Server.Theta.ShipEvent.Systems;
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
        if(_radarConsoleSystem.HasFlag(radar, RadarRenderableGroup.ShipEventTeammateGroup))
            states.AddRange(GetShipEventTeammateGroup(radar, xform));
        if(_radarConsoleSystem.HasFlag(radar, RadarRenderableGroup.Projectiles))
            states.AddRange(GetProjectileGroup(radar, xform));
        if(_radarConsoleSystem.HasFlag(radar, RadarRenderableGroup.Cannon))
            states.AddRange(GetCannonGroup(consoleUid, radar, xform));
        return states;
    }

    private List<CommonRadarEntityInterfaceState> GetShipEventTeammateGroup(RadarConsoleComponent radar, TransformComponent consoleTransform)
    {
        var states = new List<CommonRadarEntityInterfaceState>();

        var query = EntityQueryEnumerator<RadarRenderableComponent, MindContainerComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var radarRenderable, out var mindContainer, out var mobState, out var transform))
        {
            if (_mobStateSystem.IsIncapacitated(uid, mobState))
                continue;
            if (!consoleTransform.MapPosition.InRange(transform.MapPosition, radar.MaxRange))
                continue;

            Color? color = null;
            if (mindContainer.Mind != null)
            {
                foreach (var role in mindContainer.Mind.AllRoles)
                {
                    if (role is ShipEventRole && role.Faction is ShipEventFaction shipEventFaction)
                        color = shipEventFaction.Color;
                }
            }

            var coords = _transformSystem.GetMoverCoordinates(uid, transform);
            states.Add(new CommonRadarEntityInterfaceState(
                coords,
                _transformSystem.GetWorldRotation(transform),
                radarRenderable.RadarView,
                color
                )
            );
        }

        return states;
    }

    private List<CommonRadarEntityInterfaceState> GetProjectileGroup(RadarConsoleComponent radar, TransformComponent consoleTransform)
    {
        var states = new List<CommonRadarEntityInterfaceState>();
        var query = EntityQueryEnumerator<RadarRenderableComponent, ProjectileComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var radarRenderable, out _, out var transform))
        {
            if (!consoleTransform.MapPosition.InRange(transform.MapPosition, radar.MaxRange))
                continue;

            var coords = _transformSystem.GetMoverCoordinates(uid, transform);
            states.Add(new CommonRadarEntityInterfaceState(
                    coords,
                    _transformSystem.GetWorldRotation(transform),
                    radarRenderable.RadarView
                )
            );
        }
        return states;
    }

    private List<CommonRadarEntityInterfaceState> GetCannonGroup(EntityUid consoleUid, RadarConsoleComponent radar,
        TransformComponent consoleTransform)
    {
        var states = new List<CommonRadarEntityInterfaceState>();

        var myGrid = consoleTransform.GridUid;
        var isCannonConsole = HasComp<CannonConsoleComponent>(consoleUid);

        var controlledCannons = _radarConsoleSystem.GetControlledCannons(consoleUid);

        var query = EntityQueryEnumerator<RadarRenderableComponent, CannonComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var radarRenderable, out var cannon, out var transform))
        {
            if (transform.GridUid != myGrid)
                continue;
            if (!transform.Anchored)
                continue;

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

            var coords = _transformSystem.GetMoverCoordinates(uid, transform);
            states.Add(new CommonRadarEntityInterfaceState(
                    coords,
                    _transformSystem.GetWorldRotation(transform),
                    radarRenderable.RadarView,
                    mainColor
                )
            );
        }

        return states;
    }
}
