using System.IO;
using System.Linq;
using System.Resources;
using Content.Server.Theta.ShipEvent.Systems;
using Nett;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events.Theta;

public sealed class ShipEvent : StationEventSystem
{
    [Dependency] private ShipEventFactionSystem _shipSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;

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
        _shipSys.RuleSelected = true;

        var eventConfigPath = new ResourcePath("/Prototypes/Theta/Shipevent/shipevent.toml");
        if (!_resMan.TryContentFileRead(eventConfigPath, out var configStream))
            return;

        using var configReader = new StreamReader(configStream, EncodingHelpers.UTF8);
        var config = configReader.ReadToEnd().Replace(Environment.NewLine, "\n");
        var table = Toml.ReadString(config);
        
        var roundDuration = (int)((TomlInt)table["RoundDuration"]).Value;

        //maybe it's worth to automate collection of system's public variables in future
        _shipSys.RoundDuration = roundDuration;
        _shipSys.TimedRoundEnd = roundDuration > 0;
        _shipSys.TeamCheckInterval = (float)((TomlFloat)table["TeamCheckInterval"]).Value;
        _shipSys.RespawnDelay = (float)((TomlFloat)table["RespawnDelay"]).Value;
        _shipSys.MaxSpawnOffset = (int)((TomlInt)table["MaxSpawnOffset"]).Value;
        _shipSys.CollisionCheckRange = (int)((TomlInt)table["CollisionCheckRange"]).Value;
        _shipSys.BonusInterval = (int)((TomlInt)table["BonusInterval"]).Value;
        _shipSys.PointsPerInterval = (int)((TomlInt)table["PointsPerInterval"]).Value;
        _shipSys.PointsPerHitMultiplier = (float)((TomlFloat)table["PointsPerHitMultiplier"]).Value;
        _shipSys.PointsPerAssist = (int)((TomlInt)table["PointsPerAssist"]).Value;
        _shipSys.PointsPerKill = (int)((TomlInt)table["PointsPerKill"]).Value;

        _shipSys.HUDPrototypeId = ((TomlString) table["HUDPrototypeId"]).Value;
        _shipSys.ShipTypes = ((TomlArray) table["ShipTypes"]).Value.Select(s => (string)((TomlString)s).Value).ToList();
        _shipSys.ObstacleTypes = ((TomlArray) table["ObstacleTypes"]).Value.Select(s => (string) ((TomlString) s).Value).ToList();

        _shipSys.CreateObstacles((int)((TomlInt)table["InitialObstacleAmount"]).Value);
    }
}
