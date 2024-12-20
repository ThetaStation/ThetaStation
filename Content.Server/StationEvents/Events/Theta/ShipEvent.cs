using Content.Server.Theta.MapGen;
using Content.Server.Theta.MapGen.Distributions;
using Content.Server.Theta.MapGen.Generators;
using Content.Server.Theta.MapGen.Processors;
using Content.Server.Theta.MapGen.Prototypes;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.StationEvents.Events.Theta;

[RegisterComponent, Access(typeof(ShipEventRule))]
public sealed partial class ShipEventRuleComponent : Component
{
    //all time related fields are in seconds

    //time
    [DataField] public int RoundDuration; //set to negative if you don't need a timed round end
    [DataField] public float TeamCheckInterval;
    [DataField] public float FleetCheckInterval;
    [DataField] public float PlayerCheckInterval;
    [DataField] public int RespawnDelay;
    [DataField] public int BonusInterval;
    [DataField] public float BoundsCompressionInterval;
    [DataField] public float PickupsSpawnInterval;
    [DataField] public float AnomalyUpdateInterval;
    [DataField] public float AnomalySpawnInterval;
    [DataField] public float ModifierUpdateInterval;

    //points
    [DataField] public int PointsPerInterval;
    [DataField] public float PointsPerHitMultiplier;
    [DataField] public int PointsPerAssist;
    [DataField] public int PointsPerKill;
    [DataField] public int OutOfBoundsPenalty;

    //fleets
    [DataField] public int FleetMaxTeams;
    [DataField] public int FleetPointsPerTeam; //how much points across all teams in the fleet are required to raise the limit

    //mapgen
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<MapGenPresetPrototype>))]
    public string MapGenPresetPrototype;
    [DataField] public int MaxFieldSize;
    [DataField] public int MetersPerPlayer; //scaling field based on online (at roundstart)
    [DataField] public int RoundFieldSizeTo;

    //misc
    [DataField] public Color? SpaceLightColor = null;
    [DataField] public List<string> ShipTypes = new();
    [DataField] public int BoundsCompressionDistance;
    [DataField] public int PickupPositionCount;
    [DataField] public float PickupMinDistance;
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomEntityPrototype>))]
    public string PickupPrototype = default!;
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> AnomalyPrototypes = new();
    [DataField] public int ModifierAmount;
    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<ShipEventModifierPrototype>))]
    public List<string> ModifierPrototypes = new();
}

public sealed class ShipEventRule : StationEventSystem<ShipEventRuleComponent>
{
    [Dependency] private ShipEventTeamSystem _shipSys = default!;
    [Dependency] private MapGenSystem _mapGenSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private IRobustRandom _rand = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipEventRuleComponent, ComponentShutdown>(OnEventShutDown);
    }

    // for tests
    private void OnEventShutDown(Entity<ShipEventRuleComponent> ent, ref ComponentShutdown args)
    {
        _shipSys.RuleSelected = false;
    }

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

    //todo: this is absolute boilerplate, we should really make some kind of a source gen for setting those fields
    protected override void Started(EntityUid uid, ShipEventRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var mapGenPreset = _protMan.Index<MapGenPresetPrototype>(component.MapGenPresetPrototype).ShallowCopy();

        var map = _mapMan.CreateMap();
        if (component.SpaceLightColor != null)
            EnsureComp<MapLightComponent>(_mapMan.GetMapEntityId(map)).AmbientLightColor = component.SpaceLightColor.Value;
        _shipSys.TargetMap = map;

        _shipSys.RoundDuration = component.RoundDuration;
        _shipSys.TimedRoundEnd = component.RoundDuration > 0;
        _shipSys.TeamCheckInterval = component.TeamCheckInterval;
        _shipSys.FleetCheckInterval = component.FleetCheckInterval;
        _shipSys.PlayerCheckInterval = component.PlayerCheckInterval;
        _shipSys.RespawnDelay = component.RespawnDelay;
        _shipSys.BonusInterval = component.BonusInterval;

        _shipSys.PointsPerInterval = component.PointsPerInterval;
        _shipSys.PointsPerHitMultiplier = component.PointsPerHitMultiplier;
        _shipSys.PointsPerAssist = component.PointsPerAssist;
        _shipSys.PointsPerKill = component.PointsPerKill;
        _shipSys.OutOfBoundsPenalty = component.OutOfBoundsPenalty;

        _shipSys.FleetMaxTeams = component.FleetMaxTeams;
        _shipSys.FleetPointsPerTeam = component.FleetPointsPerTeam;

        _shipSys.PickupsPositionsCount = component.PickupPositionCount;
        _shipSys.PickupSpawnInterval = component.PickupsSpawnInterval;
        _shipSys.PickupMinDistance = component.PickupMinDistance;
        _shipSys.PickupPrototype = component.PickupPrototype;

        //todo: add support for non square field to ship event sys
        _shipSys.MaxSpawnOffset = mapGenPreset.Area.Width;
        var extraMeters = Math.Min(
            (int) Math.Round((float) _playerMan.PlayerCount * component.MetersPerPlayer / component.RoundFieldSizeTo) * component.RoundFieldSizeTo,
            component.MaxFieldSize);
        var areaBottomLeft = mapGenPreset.Area.BottomLeft;
        mapGenPreset.Area = mapGenPreset.Area.Translated(new Vector2i(extraMeters, extraMeters));
        mapGenPreset.Area.BottomLeft = areaBottomLeft;

        _shipSys.BoundsCompressionInterval = component.BoundsCompressionInterval;
        _shipSys.BoundsCompression = component.BoundsCompressionInterval > 0;
        _shipSys.BoundsCompressionDistance = component.BoundsCompressionDistance;

        foreach (var shipTypeProtId in component.ShipTypes)
        {
            _shipSys.ShipTypes.Add(_protMan.Index<ShipTypePrototype>(shipTypeProtId));
        }

        _shipSys.AnomalyUpdateInterval = component.AnomalyUpdateInterval;
        _shipSys.AnomalySpawnInterval = component.AnomalySpawnInterval;
        foreach (var anomalyProtId in component.AnomalyPrototypes)
        {
            _shipSys.AnomalyPrototypes.Add(_protMan.Index<EntityPrototype>(anomalyProtId));
        }

        _shipSys.ModifierUpdateInterval = component.ModifierUpdateInterval;
        _shipSys.ModifierAmount = component.ModifierAmount;
        foreach (var modifierProtId in component.ModifierPrototypes)
        {
            _shipSys.AllModifiers.Add(_protMan.Index<ShipEventModifierPrototype>(modifierProtId));
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

        mapGenPreset.GlobalProcessors.AddRange([iffSplitProc, iffFlagProc]);
        _shipSys.ShipProcessors.Add(iffSplitProc);

        _mapGenSys.RunPreset(map, mapGenPreset);
        _shipSys.RuleSelected = true;
    }
}
