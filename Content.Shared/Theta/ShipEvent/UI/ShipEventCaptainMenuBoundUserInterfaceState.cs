using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class ShipEventCaptainMenuBoundUserInterfaceState : BoundUserInterfaceState
{
    public ShipTypePrototype? CurrentShipType;
    public List<string> Members;

    public ShipEventCaptainMenuBoundUserInterfaceState(List<string> members, ShipTypePrototype? currentShipType)
    {
        Members = members;
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
public sealed class ShipEventCaptainMenuChangeBlacklistMessage : BoundUserInterfaceMessage
{
    public List<string> NewBlacklist;

    public ShipEventCaptainMenuChangeBlacklistMessage(List<string> newBlacklist)
    {
        NewBlacklist = newBlacklist;
    }
}

[Serializable, NetSerializable]
public sealed class ShipEventCaptainMenuKickMemberMessage : BoundUserInterfaceMessage
{
    public string CKey;

    public ShipEventCaptainMenuKickMemberMessage(string ckey)
    {
        CKey = ckey;
    }
}

[Serializable, NetSerializable]
public enum CaptainMenuUiKey
{
    Key
}
