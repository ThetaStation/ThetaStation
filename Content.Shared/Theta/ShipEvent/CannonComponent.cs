using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Theta.ShipEvent;

[NetworkedComponent, AutoGenerateComponentState, RegisterComponent]
public sealed partial class CannonComponent : Component
{
    /// <summary>
    /// TurretLoader bound to this turret
    /// </summary>
    public TurretLoaderComponent? BoundLoader;
    
    /// <summary>
    /// The TurretLoader's entity.
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? BoundLoaderEntity;

    /// <summary>
    /// Ammo prototypes which this turret can use
    /// </summary>
    [DataField("ammoPrototypes"), ViewVariables(VVAccess.ReadWrite)] 
    public List<string> AmmoPrototypes = new();

    /// <summary>
    /// Obstructed sectors around cannon, to prevent projectiles from colliding with the ship
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public List<(Angle, Angle)> ObstructedRanges = new();
}
