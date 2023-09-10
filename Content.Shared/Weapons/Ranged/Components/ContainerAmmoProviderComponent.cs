using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
///     Handles pulling entities from the given container to use as ammunition.
/// </summary>
[RegisterComponent]
public sealed partial class ContainerAmmoProviderComponent : AmmoProviderComponent
{
    [DataField("container", required: true)]
    [AutoNetworkedField]
    [ViewVariables]
    public string Container = "storagebase";

    [DataField("provider")]
    [AutoNetworkedField]
    [ViewVariables]
    public EntityUid? ProviderUid;
}
