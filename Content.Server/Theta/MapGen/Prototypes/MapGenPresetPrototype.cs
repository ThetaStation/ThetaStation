using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Theta.MapGen.Prototypes;

[Prototype("mapgenlayer")]
public sealed class MapGenLayerPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = "";

    [DataField(required: true)]
    public int StructureAmount;

    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<MapGenStructurePrototype>))]
    public List<string> Structures = new();

    /// <summary>
    /// If not specified distribution from the previous layer will be used
    /// </summary>
    [DataField]
    public IMapGenDistribution? Distribution = null;
}

[Prototype("mapgenpreset")]
public sealed class MapGenPresetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = "";

    [DataField]
    public Box2i Area;

    [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<MapGenLayerPrototype>))]
    public List<string> Layers = new();

    [DataField]
    public List<IMapGenProcessor> GlobalProcessors = new();

    public MapGenPresetPrototype ShallowCopy()
    {
        return (MapGenPresetPrototype) MemberwiseClone();
    }
}