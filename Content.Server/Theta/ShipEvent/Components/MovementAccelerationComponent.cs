namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed class MovementAccelerationComponent : Component
{
    [DataField("acceleration")] public float Acceleration;
    
    [DataField("maxVelocity")] public float MaxVelocity;
}
