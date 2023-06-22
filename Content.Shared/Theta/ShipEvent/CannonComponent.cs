using Content.Shared.Theta.ShipEvent.Components;

namespace Content.Shared.Theta.ShipEvent;

[RegisterComponent]
public sealed class CannonComponent : Component
{
    /// <summary>
    /// TurretLoader bound to this turret
    /// </summary>
    public TurretLoaderComponent? BoundLoader;
    
    /// <summary>
    /// The TurretLoader's entity.
    public EntityUid? BoundLoaderEntity;

    /// <summary>
    /// Ammo prototypes which this turret can use
    /// </summary>
    [DataField("ammoPrototypes"), ViewVariables(VVAccess.ReadWrite)] 
    public List<string> AmmoPrototypes = new();

    /// <summary>
    /// Unobstructed circle sectors around cannon, to avoid collisions with ship
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<(Angle, Angle)> FreeFiringRanges = new();
}
