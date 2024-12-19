using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class AdmiralMenuManageTeamMessage : BoundUserInterfaceMessage
{
    public string Name = string.Empty;
}

[Serializable, NetSerializable]
public sealed class AdmiralMenuCreateTeamMessage : BoundUserInterfaceMessage
{
    public string Name = string.Empty;
}

[Serializable, NetSerializable]
public sealed class AdmiralMenuBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<TeamInterfaceState> Teams = new();
}

[Serializable, NetSerializable]
public enum AdmiralMenuUiKey
{
    Key
}
