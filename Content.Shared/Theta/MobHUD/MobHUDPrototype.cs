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
    
    public bool Equals(MobHUDPrototype? other)
    {
        if (other == null){return false;}
        return ID == other.ID;
    }
}