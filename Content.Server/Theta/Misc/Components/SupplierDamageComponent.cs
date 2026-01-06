using Content.Shared.Damage;

namespace Content.Server.Theta.Misc.Components;

[RegisterComponent]
public sealed partial class SupplierDamageComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float DamageMultiplier;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan UpdateInterval;

    public TimeSpan NextUpdate;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage;
}
