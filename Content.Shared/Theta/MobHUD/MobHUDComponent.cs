using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.MobHUD;

[RegisterComponent]
[NetworkedComponent]
public sealed class MobHUDComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public List<MobHUDPrototype> ActiveHUDs = new();
}

[Serializable, NetSerializable]
public sealed class MobHUDState : ComponentState
{
    public List<MobHUDPrototype> ActiveHUDs = default!;
}
