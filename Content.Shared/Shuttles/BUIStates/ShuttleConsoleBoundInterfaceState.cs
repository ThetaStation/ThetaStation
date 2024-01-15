using Content.Shared.Shuttles.Systems;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ShuttleConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    /// <summary>
    /// The current FTL state.
    /// </summary>
    public readonly FTLState FTLState;

    /// <summary>
    ///  When the next FTL state change happens.
    /// </summary>
    public readonly TimeSpan FTLTime;

    public List<(NetEntity Entity, string Destination, bool Enabled)> Destinations;

    public ShuttleConsoleBoundInterfaceState(
        FTLState ftlState,
        TimeSpan ftlTime,
        List<(NetEntity Entity, string Destination, bool Enabled)> destinations,
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<CannonInformationInterfaceState> cannons,
        List<DoorInterfaceState> doors,
        List<CommonRadarEntityInterfaceState> common,
        List<ShieldInterfaceState> shields) : base(maxRange, coordinates, angle, docks, cannons, doors, common, shields)
    {
        FTLState = ftlState;
        FTLTime = ftlTime;
        Destinations = destinations;
    }
}
