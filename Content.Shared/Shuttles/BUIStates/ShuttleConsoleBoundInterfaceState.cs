using Content.Shared.Shuttles.Components;
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

    public List<(EntityUid Entity, string Destination, bool Enabled)> Destinations;

    public ShuttleConsoleBoundInterfaceState(
        FTLState ftlState,
        TimeSpan ftlTime,
        List<(EntityUid Entity, string Destination, bool Enabled)> destinations,
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<MobInterfaceState> mobs,
        List<ProjectilesInterfaceState> projectiles,
        List<CannonInformationInterfaceState> cannons) : base(maxRange, coordinates, angle, docks, mobs, projectiles, cannons)
    {
        FTLState = ftlState;
        FTLTime = ftlTime;
        Destinations = destinations;
    }
}
