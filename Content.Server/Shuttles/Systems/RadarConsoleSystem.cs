using System.Linq;
using Content.Server.Mind.Components;
using Content.Server.Storage.Components;
using Content.Server.Theta.ShipEvent.Console;
using System.Numerics;
using Content.Server.Theta.RadarRenderable;
using Content.Server.UserInterface;
using Content.Shared.DeviceLinking;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Shuttles.Systems;

//everything related to cannons should be moved out of here, but this will require rewriting big parts of cannon code, which I don't want to do currently
//todo: do something about this
public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    private const string OutputPortName = "CannonConsoleSender";

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

    public List<MobInterfaceState> GetMobsAround(RadarConsoleComponent component)
    {
        var list = new List<MobInterfaceState>();

        if (!TryComp<TransformComponent>(component.Owner, out var xform))
            return list;

        var query = EntityQueryEnumerator<MindContainerComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var mobState, out var transform))
        {
            if (_mobStateSystem.IsIncapacitated(uid, mobState))
                continue;
            if (!xform.MapPosition.InRange(transform.MapPosition, component.MaxRange))
                continue;

            var coords = _transformSystem.GetMoverCoordinates(uid, transform);
            list.Add(new MobInterfaceState
            {
                Coordinates = coords,
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

    public List<CommonRadarEntityInterfaceState> GetAllAround(RadarConsoleComponent component)
    {
        var list = new List<CommonRadarEntityInterfaceState>();
        if (!TryComp<TransformComponent>(component.Owner, out var xform))
            return list;

        foreach (var (radarRenderable, transform) in EntityManager.EntityQuery<RadarRenderableComponent, TransformComponent>())
        {
            if (!xform.MapPosition.InRange(transform.MapPosition, component.MaxRange))
                continue;

            list.Add(new CommonRadarEntityInterfaceState(
                transform.Coordinates,
                _transformSystem.GetWorldRotation(xform),
                radarRenderable.RadarView
                ));
        }

        return list;
    }

    public List<CannonInformationInterfaceState> GetCannonInfosByMyGrid(RadarConsoleComponent component)
    {
        var list = new List<CannonInformationInterfaceState>();

        var myGrid = Transform(component.Owner).GridUid;
        var isCannonConsole = HasComp<CannonConsoleComponent>(component.Owner);

        var controlledCannons = GetControlledCannons(component.Owner);

        foreach (var (cannon, transform) in EntityQuery<CannonComponent, TransformComponent>())
        {
            if (transform.GridUid != myGrid)
                continue;
            if (!transform.Anchored)
                continue;

            var controlled = false;
            if (controlledCannons != null)
                controlled = controlledCannons.Contains(cannon.Owner);

            var color = controlled ? Color.Lime : (isCannonConsole ? Color.LightGreen : Color.YellowGreen);

            var ammoCountEv = new GetAmmoCountEvent();
            RaiseLocalEvent(cannon.Owner, ref ammoCountEv);

            int maxCapacity = 0;
            int usedCapacity = 0;

            if (cannon.AmmoProvider != null)
            {
                if (cannon.AmmoProvider is ContainerAmmoProviderComponent cprov)
                {
                    if (EntityManager.TryGetComponent(cprov.ProviderUid, out ServerStorageComponent? storage))
                    {
                        maxCapacity = storage.StorageCapacityMax;
                        usedCapacity = storage.StorageUsed;
                    }
                }
                else
                {
                    maxCapacity = ammoCountEv.Capacity;
                    usedCapacity = ammoCountEv.Count;
                }
            }

            list.Add(new CannonInformationInterfaceState
            {
                Uid = cannon.Owner,
                Coordinates = transform.Coordinates,
                Color = color,
                Angle = _transformSystem.GetWorldRotation(transform),
                IsControlling = controlled,
                Ammo = ammoCountEv.Count,
                UsedCapacity = usedCapacity,
                MaxCapacity = maxCapacity
            });
        }

        return list;
    }

    private List<EntityUid>? GetControlledCannons(EntityUid uid)
    {
        if (TryComp<DeviceLinkSourceComponent>(uid, out var linkSource))
        {
            //todo: we should store info about controlled cannons in the console component itself, instead of using THIS
            if(linkSource.Outputs.Keys.Contains(OutputPortName)) //using Keys.Contains() instead of ContainsKeys() because of retarded access restrictions
                return linkSource.Outputs[OutputPortName].ToList();
        }

        return null;
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

        var mobs = GetMobsAround(component);
        var projectiles = GetProjectilesAround(component);
        var cannons = GetCannonInfosByMyGrid(component);
        var all = GetAllAround(component);

        var radarState = new RadarConsoleBoundInterfaceState(
            component.MaxRange,
            coordinates,
            angle,
            new List<DockingInterfaceState>(),
            mobs,
            projectiles,
            cannons,
            all
        );

        _uiSystem.TrySetUiState(uid, RadarConsoleUiKey.Key, radarState);
    }
}
