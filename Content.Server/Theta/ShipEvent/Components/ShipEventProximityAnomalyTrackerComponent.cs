namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipEventProximityAnomalyTrackerComponent : Component
{
    public EntityUid TrackedBy;
}