using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Theta.ShipEvent.Systems;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Events.Theta;

public sealed class ShipEventRuleConfiguration : StationEventRuleConfiguration
{
    //all time related fields are in seconds
    
    [DataField("roundDuration")] public int RoundDuration; //set to negative if you don't need a timed round end

    [DataField("teamCheckInterval")] public float TeamCheckInterval;

    [DataField("respawnDelay")] public int RespawnDelay;

    [DataField("initialObstacleAmount")] public int InitialObstacleAmount;

    [DataField("maxSpawnOffset")] public int MaxSpawnOffset;
    
    [DataField("collisionCheckRange")] public int CollisionCheckRange;
    
    [DataField("bonusInterval")] public int BonusInterval;
    
    [DataField("pointsPerInterval")] public int PointsPerInterval;
    
    [DataField("pointsPerHitMultiplier")] public float PointsPerHitMultiplier;
    
    [DataField("pointsPerAssist")] public int PointsPerAssist;
    
    [DataField("pointsPerKill")] public int PointsPerKill;
    
    [DataField("hudPrototypeId")] public string HUDPrototypeId = "";
    
    [DataField("shipTypes")] public List<string> ShipTypes = new();
    
    [DataField("obstacleTypes")] public List<string> ObstacleTypes = new();
}

public sealed class ShipEvent : StationEventSystem
{
    [Dependency] private ShipEventFactionSystem _shipSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IResourceManager _resMan = default!;
    private ShipEventRuleConfiguration eventConfig = default!;

    public override string Prototype => "ShipEvent";

    public override void Started()
    {
        base.Started();

        if (Configuration is not ShipEventRuleConfiguration ev)
            return;

        eventConfig = ev;

        var map = _mapMan.CreateMap();
        _shipSys.TargetMap = map;
        _shipSys.RuleSelected = true;
        
        _shipSys.RoundDuration = eventConfig.RoundDuration;
        _shipSys.TimedRoundEnd = eventConfig.RoundDuration > 0;
        _shipSys.TeamCheckInterval = eventConfig.TeamCheckInterval;
        _shipSys.RespawnDelay = eventConfig.RespawnDelay;
        _shipSys.MaxSpawnOffset = eventConfig.MaxSpawnOffset;
        _shipSys.CollisionCheckRange = eventConfig.CollisionCheckRange;
        _shipSys.BonusInterval = eventConfig.BonusInterval;
        _shipSys.PointsPerInterval = eventConfig.PointsPerInterval;
        _shipSys.PointsPerHitMultiplier = eventConfig.PointsPerHitMultiplier;
        _shipSys.PointsPerAssist = eventConfig.PointsPerAssist;
        _shipSys.PointsPerKill = eventConfig.PointsPerKill;
                
        _shipSys.HUDPrototypeId = eventConfig.HUDPrototypeId;
        _shipSys.ShipTypes = eventConfig.ShipTypes;
        _shipSys.ObstacleTypes = eventConfig.ObstacleTypes;

        _shipSys.CreateObstacles(eventConfig.InitialObstacleAmount);
    }
}
