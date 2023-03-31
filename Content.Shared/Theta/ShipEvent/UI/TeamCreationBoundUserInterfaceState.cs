using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class TeamCreationBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly string UserMessage;

    public TeamCreationBoundUserInterfaceState(string userMessage)
    {
        UserMessage = userMessage;
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
public enum TeamCreationUiKey
{
    Key
}
