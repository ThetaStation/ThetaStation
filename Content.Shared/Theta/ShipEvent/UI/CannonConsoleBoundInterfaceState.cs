using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class CannonConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public List<CannonInformationInterfaceState> Cannons;

    public CannonConsoleBoundInterfaceState(
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<CannonInformationInterfaceState> cannons,
        List<CommonRadarEntityInterfaceState> common) : base(maxRange, coordinates, angle, docks, common)
    {
        Cannons = cannons;
    }
}

[Serializable, NetSerializable]
public enum CannonConsoleUiKey : byte
{
    Key
}
