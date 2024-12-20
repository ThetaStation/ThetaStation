using System.Threading;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Roles.Theta;
using Robust.Shared.Audio;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipEventPointMinerComponent : Component
{
    public ShipEventTeam? OwnerTeam;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OverrideDelay;

    /// <summary>
    /// Use system's method to set
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), Access(typeof(ShipEventPointMinerSystem))]
    public int Interval; //seconds

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PointsPerInterval;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier FireSound;

    public CancellationTokenSource TimerTokenSource = new();
}
