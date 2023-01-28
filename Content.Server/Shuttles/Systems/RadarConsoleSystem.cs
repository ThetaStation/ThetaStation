using Content.Server.UserInterface;
using Content.Shared.Humanoid;
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Theta.ShipEvent;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

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

        // TODO: replace HumanoidAppearanceComponent on the component denoting the player any species
        foreach (var (_, transform) in EntityManager.EntityQuery<HumanoidAppearanceComponent, TransformComponent>())
        {
            if (!xform.MapPosition.InRange(transform.MapPosition, component.MaxRange))
                continue;

            list.Add(new MobInterfaceState()
            {
                Coordinates = transform.Coordinates,
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
                Angle = _transformSystem.GetWorldRotation(xform),
            });
        }

        return list;
    }

    public List<CannonInterfaceState> GetCannonsOnGrid(RadarConsoleComponent component,
        List<EntityUid>? controlledCannons)
    {
        var list = new List<CannonInterfaceState>();
        var myGrid = Transform(component.Owner).GridUid;
        foreach (var (cannon, transform) in EntityQuery<CannonComponent, TransformComponent>())
        {
            if (transform.GridUid != myGrid)
                continue;
            var color = Color.YellowGreen;
            if (controlledCannons != null)
            {
                color = controlledCannons.Contains(cannon.Owner) ? Color.Lime : Color.LightGreen;
            }

            list.Add(new CannonInterfaceState
            {
                Coordinates = transform.Coordinates,
                Color = color,
                Angle = _transformSystem.GetWorldRotation(transform),
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
        while (!onGrid && !xform.ParentUid.IsValid())
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
        var cannons = GetCannonsOnGrid(component, null);

        var radarState = new RadarConsoleBoundInterfaceState(
            component.MaxRange,
            coordinates,
            angle,
            new List<DockingInterfaceState>(),
            mobs,
            projectiles,
            cannons
        );

        _uiSystem.TrySetUiState(component.Owner, RadarConsoleUiKey.Key, radarState);
    }
}
