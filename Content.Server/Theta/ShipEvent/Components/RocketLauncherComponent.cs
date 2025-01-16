using System.Numerics;

namespace Content.Server.Theta.ShipEvent.Components;

//guided rocket launcher
[RegisterComponent]
public sealed partial class RocketLauncherComponent : Component
{
    public List<Vector2>? Waypoints;
}
