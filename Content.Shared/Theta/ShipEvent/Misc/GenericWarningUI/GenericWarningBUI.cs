using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Misc.GenericWarningUI;

[Serializable, NetSerializable]
public sealed class GenericWarningBoundUserInterfaceState : BoundUserInterfaceState
{
}

[Serializable, NetSerializable]
public sealed class GenericWarningYesPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class GenericWarningNoPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public enum GenericWarningUiKey
{
    Key
}
