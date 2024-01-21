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
}
