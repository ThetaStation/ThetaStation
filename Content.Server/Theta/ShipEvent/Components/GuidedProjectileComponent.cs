using System.Numerics;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class GuidedProjectileComponent : Component
{
    [DataField]
    public float Velocity = 35f; //current max

    public List<Vector2> Waypoints;

    public int CurrentWaypoint;

    public TimeSpan NextCourseUpdate;
}