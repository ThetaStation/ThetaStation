using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.Misc;

[Serializable, NetSerializable]
public sealed partial class ShipEventPointMinerOverrideFinished : DoAfterEvent
{
    public string Team;

    public override DoAfterEvent Clone() => this;
}