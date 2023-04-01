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
    public readonly string Color;

    public TeamCreationRequest(string name, string blacklist, string color)
    {
        Name = name;
        Blacklist = blacklist;
        Color = color;
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
public enum TeamCreationUiKey
{
    Key
}
