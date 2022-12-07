using System.Linq;

namespace Content.Shared.Explosion.ExplosionTypes;

public sealed class EmpTimerSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var timers = EntityManager.EntityQuery<EmpTimerComponent>();
        foreach(EmpTimerComponent timer in timers)
        {
            timer.TimeRemaining -= frameTime;
            if (timer.TimeRemaining <= 0)
            {
                RemComp<EmpTimerComponent>(timer.Owner);
                var ev = new EmpTimerEndEvent();
                EntityManager.EventBus.RaiseLocalEvent(timer.Owner, ref ev);
            }
        }
    }
}
