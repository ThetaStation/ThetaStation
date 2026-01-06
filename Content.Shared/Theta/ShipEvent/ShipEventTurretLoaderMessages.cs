using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent;

[Serializable, NetSerializable]
public sealed class TurretLoaderSyncMessage : EntityEventArgs
{
    public NetEntity LoaderUid;
}
