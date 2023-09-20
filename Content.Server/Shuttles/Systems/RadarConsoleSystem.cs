using Content.Server.Storage.Components;
using Content.Server.Theta.RadarRenderable;
using Content.Server.UserInterface;
using Content.Shared.DeviceLinking;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using System.Linq;
using System.Numerics;

namespace Content.Server.Shuttles.Systems;

//everything related to cannons should be moved out of here, but this will require rewriting big parts of cannon code, which I don't want to do currently
//todo: do something about this
public sealed class RadarConsoleSystem : SharedRadarConsoleSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly RadarRenderableSystem _radarRenderable = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

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
    public List<ShieldInterfaceState> GetShieldsAround(RadarConsoleComponent component)
    {
        var list = new List<ShieldInterfaceState>();

        if (!TryComp<TransformComponent>(component.Owner, out var xform))
            return list;

        var query = EntityQueryEnumerator<CircularShieldComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var shield, out var transform))
        {
            if (!shield.Enabled)
                continue;
            if (!xform.MapPosition.InRange(transform.MapPosition, component.MaxRange))
                continue;

            list.Add(new ShieldInterfaceState
            {
                Coordinates =  _transformSystem.GetMoverCoordinates(uid, transform),
                WorldRotation = _transformSystem.GetWorldRotation(transform),
                Powered = shield.Powered,
                Angle = shield.Angle,
                Width = shield.Width,
                MaxWidth = shield.MaxWidth,
                Radius = shield.Radius,
                MaxRadius = shield.MaxRadius,
                IsControlling = false,
            });
        }

        return list;
    }

    public List<CannonInformationInterfaceState> GetCannonInfosByMyGrid(EntityUid uid, RadarConsoleComponent component)
    {
        var list = new List<CannonInformationInterfaceState>();

        var myGrid = Transform(uid).GridUid;

        var controlledCannons = GetControlledCannons(uid);
        foreach (var (cannon, transform) in EntityQuery<CannonComponent, TransformComponent>())
        {
            if (transform.GridUid != myGrid)
                continue;
            if (!transform.Anchored)
                continue;

            var controlled = false;
            if (controlledCannons != null)
                controlled = controlledCannons.Contains(cannon.Owner);

            var ammoCountEv = new GetAmmoCountEvent();
            RaiseLocalEvent(cannon.Owner, ref ammoCountEv);

            var (usedCapacity, maxCapacity) = GetCannonAmmoCount(cannon.Owner, cannon);

            list.Add(new CannonInformationInterfaceState
            {
                Uid = cannon.Owner,
                IsControlling = controlled,
                Ammo = ammoCountEv.Count,
                UsedCapacity = usedCapacity,
                MaxCapacity = maxCapacity
            });
        }

        return list;
    }

    public List<DoorInterfaceState> GetDoorInfoByMyGrid(EntityUid uid, RadarConsoleComponent component)
    {
        var list = new List<DoorInterfaceState>();

        var myGrid = Transform(uid).GridUid;

        foreach (var door in EntityQuery<TransformComponent>())
        {
            if (door.GridUid != myGrid)
                continue;

            list.Add(new DoorInterfaceState { Uid = door.Owner });
        }

        return list;
    }

    public List<EntityUid>? GetControlledCannons(EntityUid uid)
    {
        if (TryComp<DeviceLinkSourceComponent>(uid, out var linkSource))
        {
            //todo: we should store info about controlled cannons in the console component itself, instead of using THIS
            if(linkSource.Outputs.Keys.Contains(OutputPortName)) //using Keys.Contains() instead of ContainsKeys() because of retarded access restrictions
                return linkSource.Outputs[OutputPortName].ToList();
        }

        return null;
    }

    public (int usedCapacity, int maxCapacity) GetCannonAmmoCount(EntityUid consoleUid, CannonComponent? cannon)
    {
        if (!Resolve(consoleUid, ref cannon))
            return (0, 0);

        var ammoCountEv = new GetAmmoCountEvent();
        RaiseLocalEvent(consoleUid, ref ammoCountEv);

        int maxCapacity;
        int usedCapacity;

        switch (cannon.AmmoProvider)
        {
            case null:
                return (0, 0);
            case ContainerAmmoProviderComponent cprov:
            {
                if (!EntityManager.TryGetComponent(cprov.ProviderUid, out ServerStorageComponent? storage))
                    return (0, 0);

                maxCapacity = storage.StorageCapacityMax;
                usedCapacity = storage.StorageUsed;
                break;
            }
            default:
                maxCapacity = ammoCountEv.Capacity;
                usedCapacity = ammoCountEv.Count;
                break;
        }

        return (usedCapacity, maxCapacity);
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
            coordinates,
            angle,
            new List<DockingInterfaceState>(),
            GetCannonInfosByMyGrid(uid, component),
            GetDoorInfoByMyGrid(uid, component),
            _radarRenderable.GetObjectsAround(uid, component),
            GetShieldsAround(component)
        );

        _uiSystem.TrySetUiState(uid, RadarConsoleUiKey.Key, radarState);
    }

    public bool HasFlag(RadarConsoleComponent radar, RadarRenderableGroup e)
    {
        return radar.TrackedGroups.HasFlag(e);
    }
}
