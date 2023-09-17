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
    public int MemberCount;

    public ShipPickerBoundUserInterfaceState(List<ShipTypePrototype> shipTypes, int memberCount)
    {
        ShipTypes = shipTypes;
        MemberCount = memberCount;
    }
}


//Lobby messages
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
    public readonly bool HasPassword;
    public readonly int MaxMembers;

    public ShipTeamForLobbyState(string name, int members, string captain, bool hasPassword, int maxMembers)
    {
        Name = name;
        Members = members;
        Captain = captain;
        HasPassword = hasPassword;
        MaxMembers = maxMembers;
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
    public readonly string? Password;

    public JoinToShipTeamsEvent(string name, string? password)
    {
        Name = name;
        Password = password;
    }
}


//Team creation window messages
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
public sealed class TeamCreationRequest : BoundUserInterfaceMessage
{
    public readonly string Name;
    public readonly ShipTypePrototype? ShipType;
    public readonly string? Password;
    public readonly int MaxPlayers;

    public TeamCreationRequest(string name, ShipTypePrototype? shipType, string? password, int maxPlayers)
    {
        Name = name;
        ShipType = shipType;
        Password = password;
        MaxPlayers = maxPlayers;
    }
}


[Serializable, NetSerializable]
public enum TeamCreationUiKey
{
    Key
}
