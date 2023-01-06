using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Console;

[Serializable, NetSerializable]
public sealed class CannonConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public List<EntityUid> ControlledCannons;
    public CannonConsoleBoundInterfaceState(
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<MobInterfaceState> mobs,
        List<ProjectilesInterfaceState> projectiles,
        List<CannonInterfaceState> cannons,
        List<EntityUid> controlledCannons) : base(maxRange, coordinates, angle, docks, mobs, projectiles, cannons)
    {
        ControlledCannons = controlledCannons;
    }
}

[Serializable, NetSerializable]
public enum CannonConsoleUiKey : byte
{
    Key
}
