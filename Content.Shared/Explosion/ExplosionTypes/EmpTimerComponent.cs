namespace Content.Shared.Explosion.ExplosionTypes;

/// <summary>
/// Component for tracking EMP state on certain entities (headsets/power receivers/etc.)
/// </summary>
[RegisterComponent]
public sealed class EmpTimerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeRemaining = 0;
}
