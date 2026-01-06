using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public enum CannonConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CannonConsoleBUICreatedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CannonConsoleBUIDisposedMessage : BoundUserInterfaceMessage
{
}
