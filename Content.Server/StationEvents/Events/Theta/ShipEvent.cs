using Content.Server.GameTicking.Rules.Components;
using Content.Server.Theta.DebrisGeneration;
using Content.Server.Theta.DebrisGeneration.Generators;
using Content.Server.Theta.DebrisGeneration.Processors;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Content.Shared.Theta.ShipEvent;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Content.Server.StationEvents.Events.Theta;

[RegisterComponent, Access(typeof(ShipEventRule))]
public sealed class ShipEventRuleComponent : Component
{
    //all time related fields are in seconds

    [DataField("roundDuration")] public int RoundDuration; //set to negative if you don't need a timed round end

    [DataField("teamCheckInterval")] public float TeamCheckInterval;

    [DataField("respawnDelay")] public int RespawnDelay;

    [DataField("initialObstacleAmount")] public int InitialObstacleAmount;

    [DataField("maxSpawnOffset")] public int MaxSpawnOffset;

    [DataField("bonusInterval")] public int BonusInterval;

    [DataField("playersPerTeamPlace")] public int PlayersPerTeamPlace;

    [DataField("pointsPerInterval")] public int PointsPerInterval;

    [DataField("pointsPerHitMultiplier")] public float PointsPerHitMultiplier;

    [DataField("pointsPerAssist")] public int PointsPerAssist;

    [DataField("pointsPerKill")] public int PointsPerKill;

    [DataField("outOfBoundsPenalty")] public int OutOfBoundsPenalty;

    [DataField("hudPrototypeId")] public string HUDPrototypeId = "";

    [DataField("captainHudPrototypeId")] public string CaptainHUDPrototypeId = "";

    [DataField("shipTypes")] public List<string> ShipTypes = new();

    [DataField("obstacleTypes")] public List<string> ObstacleTypes = new();

    [DataField("obstacleAmountAmplitude")] public int ObstacleAmountAmplitude;

    [DataField("obstacleSizeAmplitude")] public int ObstacleSizeAmplitude;

    [DataField("boundsCompressionInterval")] public float BoundsCompressionInterval;
    
    [DataField("boundsCompressionDistance")] public int BoundsCompressionDistance;
}

public sealed class ShipEventRule : StationEventSystem<ShipEventRuleComponent>
{
    [Dependency] private ShipEventFactionSystem _shipSys = default!;
    [Dependency] private DebrisGenerationSystem _debrisSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private IRobustRandom _rand = default!;

    protected override void Started(EntityUid uid, ShipEventRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var map = _mapMan.CreateMap();
        _shipSys.TargetMap = map;
        _shipSys.RuleSelected = true;

        _shipSys.RoundDuration = component.RoundDuration;
        _shipSys.TimedRoundEnd = component.RoundDuration > 0;
        _shipSys.TeamCheckInterval = component.TeamCheckInterval;
        _shipSys.RespawnDelay = component.RespawnDelay;
        _shipSys.BonusInterval = component.BonusInterval;
        _shipSys.PlayersPerTeamPlace = component.PlayersPerTeamPlace;
        _shipSys.PointsPerInterval = component.PointsPerInterval;
        _shipSys.PointsPerHitMultiplier = component.PointsPerHitMultiplier;
        _shipSys.PointsPerAssist = component.PointsPerAssist;
        _shipSys.PointsPerKill = component.PointsPerKill;
        _shipSys.OutOfBoundsPenalty = component.OutOfBoundsPenalty;

        _shipSys.HUDPrototypeId = component.HUDPrototypeId;
        _shipSys.CaptainHUDPrototypeId = component.CaptainHUDPrototypeId;
        
        _shipSys.MaxSpawnOffset = component.MaxSpawnOffset;

        _shipSys.BoundsCompressionInterval = component.BoundsCompressionInterval;
        _shipSys.BoundsCompression = component.BoundsCompressionInterval > 0;
        _shipSys.BoundsCompressionDistance = component.BoundsCompressionDistance;

        foreach (var shipTypeProtId in component.ShipTypes)
        {
            _shipSys.ShipTypes.Add(_protMan.Index<ShipTypePrototype>(shipTypeProtId));
        }

        List<StructurePrototype> obstacleStructProts = new();
        foreach (var structProtId in component.ObstacleTypes)
        {
            var structProt = _protMan.Index<StructurePrototype>(structProtId);

            //todo: remove this horror after proper map gen adjustment system is made
            var randomSize = _rand.Next(-component.ObstacleSizeAmplitude, component.ObstacleSizeAmplitude);
            if (structProt.Generator is AsteroidGenerator gen)
            {
                var ratio = (gen.Size + randomSize) / gen.Size;
                gen.MaxCircleRadius *= ratio;
                gen.MaxCircleRadius *= ratio;
                structProt.MinDistance += randomSize;
                gen.Size += randomSize;
            }

            obstacleStructProts.Add(structProt);
        }

        AddComponentsProcessor iffInheritanceProc = new();
        iffInheritanceProc.Components = new ComponentRegistry(
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

        _debrisSys.SpawnStructures(map,
            Vector2i.Zero,
            component.InitialObstacleAmount + _rand.Next(-component.ObstacleAmountAmplitude, component.ObstacleAmountAmplitude),
            component.MaxSpawnOffset,
            obstacleStructProts,
            globalProcessors);
    }
}
