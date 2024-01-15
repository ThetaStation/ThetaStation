using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.Impostor.Events;

[Serializable, NetSerializable]
public sealed partial class ImpostorMagicBulletLoadedEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
