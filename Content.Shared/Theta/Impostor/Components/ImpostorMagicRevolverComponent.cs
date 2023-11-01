using Robust.Shared.GameStates;

namespace Content.Shared.Theta.Impostor.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ImpostorMagicRevolverComponent : Component { }

/// <summary>
/// Present both on the cartridge and fired projectile
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImpostorMagicBulletComponent : Component
{
    [DataField("marked")]
    public bool Marked;
}
