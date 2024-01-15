using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.RadarRenderable;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Theta.RadarRenderable;

[RegisterComponent]
public sealed partial class RadarRenderableComponent : Component
{
    [DataField("radarView", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<RadarEntityViewPrototype>))]
    public string RadarView = default!;

    [DataField("group", required: true)]
    public Enum Group = RadarRenderableGroup.None;
}
