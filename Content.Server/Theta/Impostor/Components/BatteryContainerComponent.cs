using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Theta.Impostor.Components;

[RegisterComponent]
public sealed partial class BatteryContainerComponent : Component
{
    [DataField("batterySlot", required: true)]
    public ItemSlot BatterySlot = new();

    [DataField("checkPanel")]
    public bool CheckWirePanel;
}
