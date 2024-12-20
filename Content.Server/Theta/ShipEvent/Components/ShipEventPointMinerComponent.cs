using Content.Shared.Roles.Theta;
using Robust.Shared.Audio;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipEventPointMinerComponent : Component
{
    public ShipEventTeam? OwnerTeam;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OverrideDelay;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Interval;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PointsPerInterval;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier FireSound;

    public TimeSpan? NextFire = null;
}
