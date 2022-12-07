using JetBrains.Annotations;
namespace Content.Shared.Explosion.ExplosionTypes;

/// <summary>
/// Raised when component with IEmpable interface gets EMP'ed
/// </summary>
[ByRefEvent]
public struct EmpEvent
{
    public float Intensity { get; }

    public EmpEvent(float intensity)
    {
        Intensity = intensity;
    }
}

/// <summary>
/// Raised when EMP timer wears off
/// </summary>
[ByRefEvent]
public struct EmpTimerEndEvent { }
