using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class ThetaShuttleConsoleBoundInterfaceState : RadarConsoleBoundInterfaceState
{
    public ShuttleMapInterfaceState MapState;

    public ThetaShuttleConsoleBoundInterfaceState(
        NavInterfaceState navState,
        ShuttleMapInterfaceState mapState,
        DockingInterfaceState dockState,
        List<CommonRadarEntityInterfaceState> common) : base(navState, dockState, common)
    {
        MapState = mapState;
    }
}
