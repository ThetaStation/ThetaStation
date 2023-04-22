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
    /// Minimal distance between this structure and other spawned ones. Don't forget about map size itself to avoid collisions
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
