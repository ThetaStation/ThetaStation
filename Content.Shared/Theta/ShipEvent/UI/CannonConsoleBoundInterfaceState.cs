using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public enum CannonConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CannonConsoleBUIStateMessage : BoundUserInterfaceMessage
{
    public bool Created; //true - bui was just created, false - bui disposed
}
