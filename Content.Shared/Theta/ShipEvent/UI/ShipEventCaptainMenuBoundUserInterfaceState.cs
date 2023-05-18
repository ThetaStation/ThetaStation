using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class ShipEventCaptainMenuBoundUserInterfaceState : BoundUserInterfaceState
{
    public ShipTypePrototype? CurrentShipType;

    public ShipEventCaptainMenuBoundUserInterfaceState(ShipTypePrototype? currentShipType)
    {
        CurrentShipType = currentShipType;
    }
}

[Serializable, NetSerializable]
public sealed class ShipEventCaptainMenuRequestInfoMessage : BoundUserInterfaceMessage{}

[Serializable, NetSerializable]
public sealed class ShipEventCaptainMenuChangeShipMessage : BoundUserInterfaceMessage
{
    public ShipTypePrototype NewShip = default!;

    public ShipEventCaptainMenuChangeShipMessage(ShipTypePrototype newShip)
    {
        NewShip = newShip;
    }
}

[Serializable, NetSerializable]
public enum CaptainMenuUiKey
{
    Key
}
