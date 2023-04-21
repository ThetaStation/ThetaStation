using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Theta.DebrisGeneration;
using Content.Server.Theta.DebrisGeneration.Processors;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Content.Server.StationEvents.Events.Theta;

public sealed class ShipEventRuleConfiguration : StationEventRuleConfiguration
{
    //all time related fields are in seconds
    
    [DataField("roundDuration")] public int RoundDuration; //set to negative if you don't need a timed round end

    [DataField("teamCheckInterval")] public float TeamCheckInterval;

    [DataField("respawnDelay")] public int RespawnDelay;

    [DataField("initialObstacleAmount")] public int InitialObstacleAmount;

    [DataField("maxSpawnOffset")] public int MaxSpawnOffset;

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
    [Dependency] private DebrisGenerationSystem _debrisSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    
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
        _shipSys.BonusInterval = eventConfig.BonusInterval;
        _shipSys.PointsPerInterval = eventConfig.PointsPerInterval;
        _shipSys.PointsPerHitMultiplier = eventConfig.PointsPerHitMultiplier;
        _shipSys.PointsPerAssist = eventConfig.PointsPerAssist;
        _shipSys.PointsPerKill = eventConfig.PointsPerKill;
                
        _shipSys.HUDPrototypeId = eventConfig.HUDPrototypeId;
        _shipSys.ShipTypes = eventConfig.ShipTypes;

        List<StructurePrototype> obstacleStructProts = new();
        foreach (var structProtId in eventConfig.ObstacleTypes)
        {
            obstacleStructProts.Add(_protMan.Index<StructurePrototype>(structProtId));
        }
        
        AddComponentsProcessor iffInheritanceProc = new();
        iffInheritanceProc.Components = new EntityPrototype.ComponentRegistry(
            new()
            {
                {
                    "InheritanceIFF", 
                    new EntityPrototype.ComponentRegistryEntry(new InheritanceIFFComponent(), new MappingDataNode())
                }
            }
        );
        
        FlagIFFProcessor iffFlagProc = new();
        iffFlagProc.Flags = new() { IFFFlags.HideLabel };
        iffFlagProc.ColorOverride = Color.Gold;
        
        List<Processor> globalProcessors = new() { iffInheritanceProc, iffFlagProc };

        _debrisSys.GenerateDebris(map,
            Vector2.Zero,
            obstacleStructProts,
            globalProcessors,
            eventConfig.InitialObstacleAmount,
            eventConfig.MaxSpawnOffset);
    }
}
