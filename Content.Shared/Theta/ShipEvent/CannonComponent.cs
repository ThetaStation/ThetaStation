using Content.Shared.Theta.ShipEvent.Components;

namespace Content.Shared.Theta.ShipEvent;

[RegisterComponent]
public sealed class CannonComponent : Component
{
    public TurretLoaderComponent? BoundLoader;
}
