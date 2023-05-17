using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Theta.MobHUD;

[Serializable]
[Prototype("mobhud")]
public sealed class MobHUDPrototype : IPrototype, IEquatable<MobHUDPrototype>
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("sprite", required: true)]
    public SpriteSpecifier Sprite { get; } = SpriteSpecifier.Invalid;

    /// <summary>
    /// HUDs which can see this type of HUD
    /// </summary>
    [DataField("allowedHUDs", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<MobHUDPrototype>))] 
    public List<string> AllowedHUDs = default!;
    
    /// <summary>
    /// Color of this HUD, in hex. Leave empty if you don't want to recolor your sprite.
    /// </summary>
    [DataField("color")]
    public Color Color = Color.White;

    public bool Equals(MobHUDPrototype? other)
    {
        if (other == null){return false;}
        return ID == other.ID;
    }

    public MobHUDPrototype ShallowCopy()
    {
        return (MobHUDPrototype)MemberwiseClone();
    }
}
