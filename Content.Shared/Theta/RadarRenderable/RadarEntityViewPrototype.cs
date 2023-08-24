using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Theta.RadarRenderable;

[Prototype("radarEntityView")]
public sealed class RadarEntityViewPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("defaultColor", required: true)]
    public Color DefaultColor;

    [DataField("form")]
    public IRadarRenderableForm OnRadarForm = default!;
}

public interface IRadarRenderableForm
{
}

[Serializable]
[DataDefinition]
public sealed class ShapeRadarForm : IRadarRenderableForm
{
    [DataField("vertices", required: true)]
    public Vector2[] Vertices = Array.Empty<Vector2>();

    [DataField("size")]
    public float Size = 1f;
}

[Serializable]
[DataDefinition]
public sealed class CircleRadarForm : IRadarRenderableForm
{
    [DataField("radius", required: true)]
    public float Radius = 1f;
}

[Serializable]
[DataDefinition]
public sealed class CharRadarForm : IRadarRenderableForm
{
    [DataField("char", required: true)]
    public char Char = '\0';

    [DataField("scale")]
    public float Scale = 1f;
}
