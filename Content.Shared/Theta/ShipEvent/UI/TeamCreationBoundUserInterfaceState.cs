using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class TeamCreationBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly string UserMessage;

    public TeamCreationBoundUserInterfaceState(string userMessage) { UserMessage = userMessage; }
}

[Serializable, NetSerializable]
public sealed class TeamCreationRequest : BoundUserInterfaceMessage
{
    public readonly string Name;
    public readonly string Blacklist;

    public TeamCreationRequest(string name, string blacklist)
    {
        Name = name;
        Blacklist = blacklist;
    }
}

[Serializable, NetSerializable]
public enum TeamCreationUiKey
{
    Key
}
