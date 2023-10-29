namespace Content.Server.Theta.Impostor.Components;

/// <summary>
/// Present both on the cartridge and fired projectile
/// </summary>
[RegisterComponent]
public sealed partial class ImpostorMagicBulletComponent : Component
{
    [DataField("marked")]
    public bool Marked;
}

[RegisterComponent]
public sealed partial class ImpostorMagicRevolverComponent : Component { }
