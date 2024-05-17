using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.RadarRenderable;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Theta.RadarRenderable;

[RegisterComponent]
public sealed partial class RadarRenderableComponent : Component
{
    [DataField("viewProtos", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<RadarEntityViewPrototype>))]
    public List<string> ViewPrototypes = default!;

    [DataField("group", required: true)]
    public Enum Group = RadarRenderableGroup.None;
}
