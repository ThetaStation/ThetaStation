using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class ShipEventCreateTeamBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly string UserMessage;

    public ShipEventCreateTeamBoundUserInterfaceState(string userMessage)
    {
        UserMessage = userMessage;
    }
}

[Serializable, NetSerializable]
public sealed class ShipPickerBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ShipType> ShipTypes;
    public int MemberCount;

    public ShipPickerBoundUserInterfaceState(List<ShipType> shipTypes, int memberCount)
    {
        ShipTypes = shipTypes;
        MemberCount = memberCount;
    }
}

[Serializable, NetSerializable]
public sealed class ShipEventLobbyBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<ShipTeamForLobbyState> Teams;

    public ShipEventLobbyBoundUserInterfaceState(List<ShipTeamForLobbyState> teams)
    {
        Teams = teams;
    }
}

[Serializable, NetSerializable]
public sealed class ShipTeamForLobbyState
{
    public readonly string Name;
    public readonly int Members;
    public readonly string Captain;

    public ShipTeamForLobbyState(string name, int members, string captain)
    {
        Name = name;
        Members = members;
        Captain = captain;
    }
}

[Serializable, NetSerializable]
public sealed class TeamCreationRequest : BoundUserInterfaceMessage
{
    public readonly string Name;
    public readonly string Blacklist;
    public readonly Color Color;
    public readonly ShipType? ShipType;

    public TeamCreationRequest(string name, Color color, string blacklist, ShipType? shipType)
    {
        Name = name;
        Blacklist = blacklist;
        Color = color;
        ShipType = shipType;
    }
}

[Serializable, NetSerializable]
public sealed class RefreshShipTeamsEvent : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class JoinToShipTeamsEvent : BoundUserInterfaceMessage
{
    public readonly string Name;

    public JoinToShipTeamsEvent(string name)
    {
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class GetShipPickerInfoMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum TeamCreationUiKey
{
    Key
}
