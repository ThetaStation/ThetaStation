using Content.Server.UserInterface;
using Content.Shared.Humanoid;
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Theta.ShipEvent;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network.Messages;

namespace Content.Server.Shuttles.Systems;

public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

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

    public List<CannonInterfaceState> GetCannonsOnGrid(RadarConsoleComponent component)
    {
        var list = new List<CannonInterfaceState>();
        var myGrid = Transform(component.Owner).GridUid;
        foreach (var cannon in EntityQuery<CannonComponent>())
        {
            var transform = Transform(cannon.Owner);
            if(transform.GridUid != myGrid)
                continue;
            list.Add(new CannonInterfaceState
            {
                Coordinates = transform.Coordinates,
                Entity = cannon.Owner,
                Angle = transform.WorldRotation
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
        var cannons = GetCannonsOnGrid(component);

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
