using Robust.Shared.Prototypes;

namespace Content.Server.Theta.MapGen.Prototypes;

[Prototype("mapgenstructure")]
public sealed class MapGenStructurePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = "";

    /// <summary>
    /// Chance to spawn this structure in current layer, from 0 to 1
    /// </summary>
    [DataField("spawnWeight", required: true)]
    public float SpawnWeight;

    /// <summary>
    /// Grid's AABB is enlarged by this value when calculating occupied space
    /// </summary>
    [DataField("minDistance", required: true)]
    public int MinDistance;

    /// <summary>
    /// Generator which will generate base grid(s) of this structure
    /// </summary>
    [DataField("generator", required: true)]
    public IMapGenGenerator Generator = default!;

    /// <summary>
    /// Processors which will be applied to base grid(s) of this structure
    /// </summary>
    [DataField("processors")]
    public IMapGenProcessor[] Processors = Array.Empty<IMapGenProcessor>();
}
