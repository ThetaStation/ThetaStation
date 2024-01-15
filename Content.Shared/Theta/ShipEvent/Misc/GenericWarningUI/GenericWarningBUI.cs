using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Misc.GenericWarningUI;

[Serializable, NetSerializable]
public sealed class GenericWarningBoundUserInterfaceState : BoundUserInterfaceState
{
    public string? TitleLoc;
    public string? WarningLoc;
    public string? YesButtonLoc;
    public string? NoButtonLoc;
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
    GenericKey,
    ShipEventKey,
}
