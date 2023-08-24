using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.RadarRenderable;

[Prototype("radarEntityView")]
public sealed class RadarEntityViewPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("defaultColor", required: true)]
    public Color DefaultColor;

    [DataField("form")]
    public Enum OnRadarForm = OnRadarForms.Circle;

    [DataField("size")]
    public float Size = 1f;

}

[Serializable, NetSerializable]
public enum OnRadarForms
{
    Circle,
    FootingTriangle,
    CenteredTriangle,
    Line,
}
