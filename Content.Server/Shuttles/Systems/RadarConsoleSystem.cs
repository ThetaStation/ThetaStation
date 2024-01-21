using Content.Server.Theta.RadarRenderable;
using Content.Server.UserInterface;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly RadarRenderableSystem _radarRenderable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, ComponentStartup>(OnRadarStartup);
    }

    private void OnRadarStartup(EntityUid uid, RadarConsoleComponent component, ComponentStartup args)
    {
        UpdateState(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadarConsoleComponent>();
        while (query.MoveNext(out var uid, out var radar))
        {
            if (!_uiSystem.IsUiOpen(uid, RadarConsoleUiKey.Key))
                continue;
            UpdateState(uid, radar);
        }
    }

    protected override void UpdateState(EntityUid uid, RadarConsoleComponent component)
    {
        var xform = Transform(uid);
        var onGrid = xform.ParentUid == xform.GridUid;
        Angle? angle = onGrid ? xform.LocalRotation : Angle.Zero;
        // find correct grid
        while (!onGrid && !xform.ParentUid.IsValid())
        {
            xform = Transform(xform.ParentUid);
            angle = Angle.Zero;
            onGrid = xform.ParentUid == xform.GridUid;
        }

        EntityCoordinates? coordinates = onGrid ? xform.Coordinates : null;

        // Use ourself I guess.
        if (TryComp<IntrinsicUIComponent>(uid, out var intrinsic))
        {
            foreach (var uiKey in intrinsic.UIs)
            {
                if (uiKey.Key?.Equals(RadarConsoleUiKey.Key) == true)
                {
                    coordinates = new EntityCoordinates(uid, Vector2.Zero);
                    angle = Angle.Zero;
                    break;
                }
            }
        }

        var radarState = new RadarConsoleBoundInterfaceState(
            component.MaxRange,
            GetNetCoordinates(coordinates),
            angle,
            new List<DockingInterfaceState>(),
            _radarRenderable.GetObjectsAround(uid, component)
        );

        _uiSystem.TrySetUiState(uid, RadarConsoleUiKey.Key, radarState);
    }

    public bool HasFlag(RadarConsoleComponent radar, RadarRenderableGroup e)
    {
        return radar.TrackedGroups.HasFlag(e);
    }
}
