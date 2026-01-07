using Robust.Shared.GameStates;

namespace Content.Shared.Theta.ShipEvent;

[NetworkedComponent, AutoGenerateComponentState(true), RegisterComponent]
public sealed partial class CannonComponent : Component
{
    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundLoaderUid;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundConsoleUid;

    [DataField("ammoPrototypes"), ViewVariables(VVAccess.ReadWrite)]
    public List<string> AmmoPrototypes = new();

    /// <summary>
    /// Obstructed sectors around cannon, to prevent projectiles from colliding with the ship
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField, DataField("ranges")]
    public List<(Angle, Angle)> ObstructedRanges = new();

    [DataField("rotatable")]
    public bool Rotatable = true;

    /// <summary>
    /// Recoil impulse, applied to parent grid after each shot
    /// todo: might be better to set this in the gun component
    /// </summary>
    [DataField("recoil")]
    public float Recoil = 0;

    /// <summary>
    /// For shotgun-like cannons
    /// todo: this should be updated based on the current ammo type
    /// </summary>
    public float Spread = 0;
}
