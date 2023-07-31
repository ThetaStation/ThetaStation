using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class ShipEventCaptainMenuBoundUserInterfaceState : BoundUserInterfaceState
{
    public ShipTypePrototype? CurrentShipType;
    public List<string> Members;
    public string? Password;
    public int MaxMembers;

    public ShipEventCaptainMenuBoundUserInterfaceState(List<string> members, ShipTypePrototype? currentShipType, string? password, int maxMembers)
    {
        Members = members;
        CurrentShipType = currentShipType;
        Password = password;
        MaxMembers = maxMembers;
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
public sealed class ShipEventCaptainMenuSetPasswordMessage : BoundUserInterfaceMessage
{
    public string? Password;

    public ShipEventCaptainMenuSetPasswordMessage(string? password)
    {
        Password = password;
    }
}

[Serializable, NetSerializable]
public sealed class ShipEventCaptainMenuSetMaxMembersMessage : BoundUserInterfaceMessage
{
    public int MaxMembers;

    public ShipEventCaptainMenuSetMaxMembersMessage(int maxMembers)
    {
        MaxMembers = maxMembers;
    }
}

[Serializable, NetSerializable]
public enum CaptainMenuUiKey
{
    Key
}
