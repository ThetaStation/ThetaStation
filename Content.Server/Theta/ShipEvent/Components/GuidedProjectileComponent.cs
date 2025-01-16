using System.Numerics;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class GuidedProjectileComponent : Component
{
    public List<Vector2> Waypoints;

    public int CurrentWaypoint;

    public TimeSpan NextCourseUpdate;
}