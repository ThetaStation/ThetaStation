using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

public sealed class TurretLoaderBoundUserInterfaceState : BoundUserInterfaceState
{
    public int AmmoCurrent;
    public int TurretUid;
    public string Status;

    public TurretLoaderBoundUserInterfaceState(int _ammoCurrent, int _turretUid, string _status)
    {
        AmmoCurrent = _ammoCurrent;
        TurretUid = _turretUid;
        Status = _status;
    }
}

[Serializable, NetSerializable]
public sealed class TurretLoaderEjectRequest : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class TurretLoaderAfterShotMessage : EntityEventArgs { }

[Serializable, NetSerializable]
public enum TurretLoaderUiKey
{
    Key
}
