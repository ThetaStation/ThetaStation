using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class CannonConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public CannonConsoleBoundInterfaceState(
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<CannonInformationInterfaceState> cannons,
        List<CommonRadarEntityInterfaceState> common,
        List<ShieldInterfaceState> shields) : base(maxRange, coordinates, angle, docks, cannons, common, shields)
    {
    }
}

[Serializable, NetSerializable]
public enum CannonConsoleUiKey : byte
{
    Key
}
