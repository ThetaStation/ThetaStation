using Content.Server.Humanoid;
using Content.Server.Projectiles.Components;
using Content.Server.UserInterface;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    private float UpdateRate = 1f;
    private float _updateDif;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadarConsoleComponent, ComponentStartup>(OnRadarStartup);
    }

    private void OnRadarStartup(EntityUid uid, RadarConsoleComponent component, ComponentStartup args)
    {
        UpdateState(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDif += frameTime;
        if (_updateDif < UpdateRate)
            return;
        _updateDif = 0f;

        foreach (var radar in EntityManager.EntityQuery<RadarConsoleComponent>())
        {
            UpdateState(radar);
        }
    }

    public List<MobInterfaceState> GetMobsAround(RadarConsoleComponent component)
    {
        var list = new List<MobInterfaceState>();

        if (!TryComp<TransformComponent>(component.Owner, out var xform))
            return list;

        foreach (var (_, transform) in EntityManager.EntityQuery<HumanoidComponent, TransformComponent>())
        {
            if (!xform.MapPosition.InRange(transform.MapPosition, component.MaxRange))
                continue;

            list.Add(new MobInterfaceState()
            {
                Coordinates = transform.Coordinates,
                Entity = transform.Owner
            });
        }

        return list;
    }

    public List<ProjectilesInterfaceState> GetProjectilesAround(RadarConsoleComponent component)
    {
        var list = new List<ProjectilesInterfaceState>();

        if (!TryComp<TransformComponent>(component.Owner, out var xform))
            return list;

        foreach (var (_, transform) in EntityManager.EntityQuery<ProjectileComponent, TransformComponent>())
        {
            if (!xform.MapPosition.InRange(transform.MapPosition, component.MaxRange))
                continue;

            list.Add(new ProjectilesInterfaceState()
            {
                Coordinates = transform.Coordinates,
                Angle = transform.WorldRotation,
                Entity = transform.Owner
            });
        }

        return list;
    }

    protected override void UpdateState(RadarConsoleComponent component)
    {
        var xform = Transform(component.Owner);
        var onGrid = xform.ParentUid == xform.GridUid;
        Angle? angle = onGrid ? xform.LocalRotation : Angle.Zero;
        // find correct grid
        while (!onGrid && !xform.ParentUid.Equals(EntityUid.Invalid))
        {
            xform = Transform(xform.ParentUid);
            angle = Angle.Zero;
            onGrid = xform.ParentUid == xform.GridUid;
        }
        EntityCoordinates? coordinates = onGrid ? xform.Coordinates : null;

        // Use ourself I guess.
        if (TryComp<IntrinsicUIComponent>(component.Owner, out var intrinsic))
        {
            foreach (var uiKey in intrinsic.UIs)
            {
                if (uiKey.Key?.Equals(RadarConsoleUiKey.Key) == true)
                {
                    coordinates = new EntityCoordinates(component.Owner, Vector2.Zero);
                    angle = Angle.Zero;
                    break;
                }
            }
        }

        var mobs = GetMobsAround(component);
        var projectiles = GetProjectilesAround(component);

        var radarState = new RadarConsoleBoundInterfaceState(
            component.MaxRange,
            coordinates,
            angle,
            new List<DockingInterfaceState>(),
            mobs,
            projectiles
            );

        _uiSystem.TrySetUiState(component.Owner, RadarConsoleUiKey.Key, radarState);
    }
}
