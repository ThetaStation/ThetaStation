using System.ComponentModel.DataAnnotations;
using Content.Shared.Theta.RadarRenderable;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Theta.RadarRenderable;

[RegisterComponent]
public sealed class RadarRenderableComponent : Component
{
    [DataField("radarView", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<RadarEntityViewPrototype>))]
    public readonly string RadarView = default!;

    [DataField("group", required: true)]
    public readonly RadarRenderableGroup Group = default!;
}
