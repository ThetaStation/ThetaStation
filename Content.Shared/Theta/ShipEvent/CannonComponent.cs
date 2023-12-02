using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Weapons.Ranged.Components;
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

    public AmmoProviderComponent? AmmoProvider;

    /// <summary>
    /// Ammo prototypes which this turret can use
    /// </summary>
    [DataField("ammoPrototypes"), ViewVariables(VVAccess.ReadWrite)] 
    public List<string> AmmoPrototypes = new();

    /// <summary>
    /// Obstructed sectors around cannon, to prevent projectiles from colliding with the ship
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField, DataField("ranges")] //datafield is for map serialization
    public List<(Angle, Angle)> ObstructedRanges = new();

    //so, when generating ranges instantly after component init/first anchor not all entities that should be present on parent grid are there,
    //which creates awkward situations when ranges are generated only for the 2 random walls.
    //todo: fix map loader to avoid this (or if it's intended behaviour to init components on half-loaded map, add some event after map was fully loaded to build ranges after it)
    public bool FirstAnchor = true;

    [DataField("rotatable")]
    public bool Rotatable = true;
}
