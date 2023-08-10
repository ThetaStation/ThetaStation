using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Console;

[Serializable, NetSerializable]
public sealed class CannonConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public CannonConsoleBoundInterfaceState(
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<MobInterfaceState> mobs,
        List<ProjectilesInterfaceState> projectiles,
        List<CannonInformationInterfaceState> cannons) : base(maxRange, coordinates, angle, docks, mobs, projectiles, cannons)
    {
    }
}

[Serializable, NetSerializable]
public enum CannonConsoleUiKey : byte
{
    Key
}
