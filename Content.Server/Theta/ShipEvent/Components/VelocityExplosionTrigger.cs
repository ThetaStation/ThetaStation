namespace Content.Server.Theta.ShipEvent.Components;

//Overrides explosive's total intensity based on body's velocity when trigger event was raised
[RegisterComponent]
public sealed class VelocityExplosionTriggerComponent : Component
{
    [DataField("minVelocity")] public int MinimumVelocity;

    [DataField("intensityMultiplier")] public float IntensityMultiplier;

    [DataField("maxIntensity")] public int MaximumIntensity;
}
