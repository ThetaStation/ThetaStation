using Content.Server.GameTicking.Rules.Components;
using Content.Server.Theta.DebrisGeneration;
using Content.Server.Theta.DebrisGeneration.Generators;
using Content.Server.Theta.DebrisGeneration.Processors;
using Content.Server.Theta.DebrisGeneration.Prototypes;
using Content.Shared.Theta.ShipEvent;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Random;
using Content.Shared.Shuttles.Components;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.StationEvents.Events.Theta;

[RegisterComponent, Access(typeof(ShipEventRule))]
public sealed partial class ShipEventRuleComponent : Component
{
    //all time related fields are in seconds

    [DataField("roundDuration")] public int RoundDuration; //set to negative if you don't need a timed round end

    [DataField("teamCheckInterval")] public float TeamCheckInterval;

    [DataField("respawnDelay")] public int RespawnDelay;

    [DataField("initialObstacleAmount")] public int InitialObstacleAmount;

    [DataField("minFieldSize")] public int MinFieldSize;

    [DataField("maxFieldSize")] public int MaxFieldSize;

    [DataField("metersPerPlayer")] public int MetersPerPlayer; //scaling field based on online (at roundstart)

    [DataField("roundFieldSizeTo")] public int RoundFieldSizeTo;

    [DataField("bonusInterval")] public int BonusInterval;

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

    [DataField("pickupsPositions")] public int PickupsPositionsCount;

    // in seconds
    [DataField("pickupsSpawnInterval")] public float PickupsSpawnInterval;

    [DataField("pickupMinDistance")] public float PickupMinDistance;

    [DataField("pickupsPrototypes", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>))]
    public string PickupsPrototypes = default!;

    //in miliseconds
    [DataField("stealthDuration")] public int StealthDuration;
    [DataField("stealthCooldown")] public int StealthCooldown;
}

public sealed class ShipEventRule : StationEventSystem<ShipEventRuleComponent>
{
    [Dependency] private ShipEventFactionSystem _shipSys = default!;
    [Dependency] private DebrisGenerationSystem _debrisSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private IRobustRandom _rand = default!;

    //Creates ComponentRegistryEntry for ChangeIFFOnSplit comp. Used by AddComponentProcessor to prevent splitted grids from getting labels.
    //todo: this is ugly hardcode, better to move it into prototype or somethin, when I will understand how
    private EntityPrototype.ComponentRegistryEntry CreateIFFCompEntry()
    {
        var iffSplitComp = new ChangeIFFOnSplitComponent();
        iffSplitComp.NewFlags = IFFFlags.HideLabel;
        iffSplitComp.Replicate = true;
        iffSplitComp.DeleteInheritedGridsDelay = 120;

        MappingDataNode mapping = new MappingDataNode(new Dictionary<DataNode, DataNode>
        {
            {new ValueDataNode("flags"), new ValueDataNode(IFFFlags.HideLabel.ToString())},
            {new ValueDataNode("replicate"), new ValueDataNode("true")}
        });

        return new EntityPrototype.ComponentRegistryEntry(iffSplitComp, mapping);
    }

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
        _shipSys.PointsPerInterval = component.PointsPerInterval;
        _shipSys.PointsPerHitMultiplier = component.PointsPerHitMultiplier;
        _shipSys.PointsPerAssist = component.PointsPerAssist;
        _shipSys.PointsPerKill = component.PointsPerKill;
        _shipSys.OutOfBoundsPenalty = component.OutOfBoundsPenalty;

        _shipSys.PickupsPositionsCount = component.PickupsPositionsCount;
        _shipSys.PickupsSpawnInterval = component.PickupsSpawnInterval;
        _shipSys.PickupMinDistance = component.PickupMinDistance;
        _shipSys.PickupsDatasetPrototype = component.PickupsPrototypes;

        _shipSys.StealthDuration = component.StealthDuration;
        _shipSys.StealthCooldown = component.StealthCooldown;

        _shipSys.HUDPrototypeId = component.HUDPrototypeId;
        _shipSys.CaptainHUDPrototypeId = component.CaptainHUDPrototypeId;

        _shipSys.MaxSpawnOffset = Math.Clamp(
            (int)Math.Round((float)_playerMan.PlayerCount * component.MetersPerPlayer / component.RoundFieldSizeTo) * component.RoundFieldSizeTo,
            component.MinFieldSize,
            component.MaxFieldSize);

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

        AddComponentsProcessor iffSplitProc = new();
        iffSplitProc.Components = new ComponentRegistry(
            new()
            {
                {"ChangeIFFOnSplit", CreateIFFCompEntry()}
            }
        );

        FlagIFFProcessor iffFlagProc = new();
        iffFlagProc.Flags = new() { IFFFlags.HideLabel };
        iffFlagProc.ColorOverride = Color.Gold;

        List<Processor> globalProcessors = new() { iffSplitProc, iffFlagProc };

        _debrisSys.SpawnStructures(map,
            Vector2i.Zero,
            component.InitialObstacleAmount + _rand.Next(-component.ObstacleAmountAmplitude, component.ObstacleAmountAmplitude),
            _shipSys.MaxSpawnOffset,
            obstacleStructProts,
            globalProcessors);

        _shipSys.ShipProcessors.Add(iffSplitProc);
    }
}
