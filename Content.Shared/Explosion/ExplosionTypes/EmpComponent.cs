namespace Content.Shared.Explosion.ExplosionTypes;

/// <summary>
/// Component indicating that this entity can be EMP'ed
/// </summary>
[RegisterComponent]
public sealed class EmpComponent : Component
{
    /// <summary>
    /// Bool which *actually* determines if entity with this component cares about EMP. Set to false for EMP resistant machines.
    /// </summary>
    public bool Enabled = true;
}
