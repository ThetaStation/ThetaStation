using Content.Server.Power.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed class BluespaceCatapultComponent : Component
{
    [DataField("launchSound")] 
    public SoundSpecifier LaunchSound = new SoundPathSpecifier("/Audio/Theta/hl-teleport.ogg");
    
    /// <summary>
    /// Maximum amount of energy that catapult can discharge during single launch. Every joule of energy is converted into a single newton of
    /// force applied to payload (for 1 second), so initial velocity is equal to 'used power * catapult's efficiency / load mass'
    /// </summary>
    [DataField("maxPowerPerLaunch")]
    public int MaxPower = 15000;

    /// <summary>
    /// Maximum error (in angles) for bearing & elevation
    /// </summary>
    [DataField("maxError")]
    public int MaxError = 5;

    /// <summary>
    /// How much of input energy is translated into force applied to payload
    /// </summary>
    [DataField("efficiency")]
    public float Efficiency = 0.1f;

    public int ChargeRate = 1000;

    public PowerConsumerComponent? Consumer;
    
    public BatteryComponent? Battery;

    public float Charge => Battery?.CurrentCharge ?? 0;
    
    public float MaxCharge => Battery?.MaxCharge ?? 0;

    public bool IsFullyCharged => Battery?.IsFullyCharged ?? false;

    [Access(typeof(BluespaceCatapultSystem))]
    public float animationTimer = 0;
}
