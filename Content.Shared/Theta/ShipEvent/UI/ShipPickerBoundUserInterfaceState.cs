using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

//Ship picker messages
[Serializable, NetSerializable]
public sealed class GetShipPickerInfoMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ShipPickerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ShipTypePrototype> ShipTypes;

    public ShipPickerBoundUserInterfaceState(List<ShipTypePrototype> shipTypes)
    {
        ShipTypes = shipTypes;
    }
}
