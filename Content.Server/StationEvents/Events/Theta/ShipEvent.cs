using Content.Server.Theta.ShipEvent.Systems;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Events.Theta;

public sealed class ShipEvent : StationEventSystem
{
    [Dependency] private ShipEventFactionSystem _shipSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;

    public override string Prototype => "ShipEvent";

    public override void Started()
    {
        base.Started();

        int mid = 1;
        for (int i = 0; i < 100; i++)
        {
            if (!_mapMan.MapExists(new MapId(mid))) { break; }
            mid++;
        }

        _mapMan.CreateMap(new MapId(mid));
        _shipSys.TargetMap = new MapId(mid);
    }
}
