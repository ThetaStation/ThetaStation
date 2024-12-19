using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

//Lobby messages
[Serializable, NetSerializable]
public sealed class LobbyBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<TeamInterfaceState> Teams;

    public LobbyBoundUserInterfaceState(List<TeamInterfaceState> teams)
    {
        Teams = teams;
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
public sealed class CreateTeamBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly string UserMessage;

    public CreateTeamBoundUserInterfaceState(string userMessage)
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
