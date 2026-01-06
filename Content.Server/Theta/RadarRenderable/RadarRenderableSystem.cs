using Content.Server.Shuttles.Systems;
using Content.Server.Theta.ShipEvent;
using Content.Server.Theta.ShipEvent.Console;
using Content.Shared.Doors.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;

namespace Content.Server.Theta.RadarRenderable;

//todo (radars): there shouldn't be a cannon/mob/door specific code
public sealed class RadarRenderableSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly RadarConsoleSystem _radarConsoleSystem = default!;
    [Dependency] private readonly CannonSystem _cannonSystem = default!;

    public List<CommonRadarEntityInterfaceState> GetObjectsAround(EntityUid consoleUid, RadarConsoleComponent? radar = null)
    {
        var states = new List<CommonRadarEntityInterfaceState>();
        if (Resolve(consoleUid, ref radar))
            states.AddRange(GetRadarRenderableStates(consoleUid, radar));

        return states;
    }

    //todo: this is bad, RadarRenderableComponent should be modified by the cannon/shipevent/mob systems
    //and this method should just collect the states
    private List<CommonRadarEntityInterfaceState> GetRadarRenderableStates(
        EntityUid consoleUid,
        RadarConsoleComponent? radar = null,
        TransformComponent? consoleForm = null)
    {
        var states = new List<CommonRadarEntityInterfaceState>();
        if (!Resolve(consoleUid, ref radar, ref consoleForm))
            return states;

        var query = EntityQueryEnumerator<RadarRenderableComponent, TransformComponent>();
        while (query.MoveNext(out var rendUid, out var renderable, out var rendForm))
        {
            if (!_radarConsoleSystem.HasFlag(radar, (RadarRenderableGroup) renderable.Group))
                continue;

            if (TryComp<IFFComponent>(rendForm.GridUid, out var iff) && (iff.Flags & IFFFlags.Hide) != 0)
                continue;

            if (!consoleForm.MapPosition.InRange(rendForm.MapPosition, radar.MaxRange))
                continue;

            CommonRadarEntityInterfaceState? state;
            switch ((RadarRenderableGroup) renderable.Group)
            {
                case RadarRenderableGroup.ShipEventTeammate:
                    state = GetMobState(rendUid, renderable, rendForm);
                    break;
                case RadarRenderableGroup.Cannon:
                    state = GetCannonState(rendUid, consoleUid, renderable, rendForm, consoleForm);
                    break;
                case RadarRenderableGroup.Door:
                    state = GetDoorState(rendUid, renderable, rendForm, consoleForm);
                    break;
                default:
                    state = GetDefaultState(rendUid, renderable, rendForm);
                    break;
            }

            if (state != null)
                states.Add(state);
        }

        return states;
    }

    private CommonRadarEntityInterfaceState? GetCannonState(EntityUid uid, EntityUid consoleUid,
        RadarRenderableComponent radarRenderable, TransformComponent xform, TransformComponent consoleTransform)
    {
        if (!TryComp<CannonComponent>(uid, out var cannon))
            return null;

        var consoleGrid = consoleTransform.GridUid;
        var isCannonConsole = HasComp<CannonConsoleComponent>(consoleUid);

        if (xform.GridUid == null || !xform.Anchored)
            return null;

        if (TryComp<ShipEventTeamMarkerComponent>(consoleGrid, out var consoleMarker) &&
            consoleMarker.Team != null &&
            !consoleMarker.Team.ShipGrids.Contains(xform.GridUid.Value))
            return null;

        var (ammo, maxAmmo) = _cannonSystem.GetCannonAmmoCount(uid, cannon);
        var mainColor = (cannon.BoundConsoleUid == consoleUid) ? Color.Lime : (isCannonConsole ? Color.LightGreen : Color.YellowGreen);

        var hsvColor = Color.ToHsv(mainColor);
        const float additionalDegreeCoeff = 20f / 360f;
        // X is hue
        var hueOffset = hsvColor.X * ammo / Math.Max(1, maxAmmo);
        hsvColor.X = Math.Max(hueOffset + additionalDegreeCoeff, additionalDegreeCoeff);

        mainColor = Color.FromHsv(hsvColor);


        return new CommonRadarEntityInterfaceState(
            GetNetCoordinates(_transformSystem.GetMoverCoordinates(uid, xform)),
            _transformSystem.GetWorldRotation(xform),
            radarRenderable.ViewPrototypes,
            mainColor
        );
    }

    private CommonRadarEntityInterfaceState? GetDefaultState(EntityUid uid, RadarRenderableComponent renderable,
        TransformComponent xform)
    {
        return new CommonRadarEntityInterfaceState(
            GetNetCoordinates(_transformSystem.GetMoverCoordinates(uid, xform)),
            _transformSystem.GetWorldRotation(xform),
            renderable.ViewPrototypes,
            renderable.OverrideColor
        );
    }

    private CommonRadarEntityInterfaceState? GetMobState(EntityUid uid, RadarRenderableComponent renderable, TransformComponent xform)
    {
        if (_mobStateSystem.IsIncapacitated(uid))
            return null;

        Color? color = null;
        if (TryComp<ShipEventTeamMarkerComponent>(uid, out var marker) && marker.Team != null)
            color = marker.Team.Color;

        return new CommonRadarEntityInterfaceState(
            GetNetCoordinates(_transformSystem.GetMoverCoordinates(uid, xform)),
            _transformSystem.GetWorldRotation(xform),
            renderable.ViewPrototypes,
            color
        );
    }

    private CommonRadarEntityInterfaceState? GetDoorState(EntityUid uid, RadarRenderableComponent renderable,
        TransformComponent xform, TransformComponent consoleForm)
    {
        if (Transform(uid).GridUid != consoleForm.GridUid)
            return null;

        if (!Transform(uid).Anchored || !TryComp<DoorComponent>(uid, out var door))
            return null;

        Color? color = Color.Red;
        if (door.State == DoorState.Open)
            color = Color.LimeGreen;

        return new CommonRadarEntityInterfaceState(
            GetNetCoordinates(_transformSystem.GetMoverCoordinates(uid, xform)),
            _transformSystem.GetWorldRotation(xform),
            renderable.ViewPrototypes,
            color
        );
    }
}
