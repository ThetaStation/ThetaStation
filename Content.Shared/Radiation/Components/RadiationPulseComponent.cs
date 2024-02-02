using Content.Shared.Radiation.Systems;

namespace Content.Shared.Radiation.Components;

/// <summary>
///     Create circle pulse animation of radiation around object.
///     Drawn on client after creation only once per component lifetime.
/// </summary>
[RegisterComponent]
[Access(typeof(RadiationPulseSystem))]
public sealed partial class RadiationPulseComponent : Component
{
    /// <summary>
    ///     Timestamp when component was assigned to this entity.
    /// </summary>
    public TimeSpan StartTime;

    /// <summary>
    ///     The range of animation.
    /// </summary>
    [DataField("visualRange")]
    public float VisualRange = 5f;

    /// <summary>
    /// If set, VisualRange will be overriden by radiation source, so effect covers whole irradiated area.
    /// </summary>
    [DataField("autoRange")]
    public bool AutoRange = true;
}
