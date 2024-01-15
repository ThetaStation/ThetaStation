using Robust.Shared.Prototypes;

namespace Content.Server.Theta.DebrisGeneration.Prototypes;

[Prototype("structure")]
public sealed class StructurePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = "";

    /// <summary>
    /// Chance to spawn this structure, from 0 to 1
    /// </summary>
    [DataField("spawnWeight", required: true)]
    public float SpawnWeight;

    /// <summary>
    /// Minimal distance between this structure and other spawned ones. Set this to 0 if you only want to take collision distance into account
    /// </summary>
    [DataField("minDistance", required: true)]
    public int MinDistance;

    /// <summary>
    /// Generator which will generate base grid of this structure
    /// </summary>
    [DataField("generator", required: true)]
    public Generator Generator = default!;

    /// <summary>
    /// Processors which will be applied to base grid of this structure
    /// </summary>
    [DataField("processors")]
    public Processor[] Processors = Array.Empty<Processor>();
}
