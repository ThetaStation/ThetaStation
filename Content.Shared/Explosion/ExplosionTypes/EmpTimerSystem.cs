namespace Content.Shared.Explosion.ExplosionTypes;

public sealed class EmpTimerSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach(EmpTimerComponent timer in EntityQuery<EmpTimerComponent>())
        {
            timer.TimeRemaining -= frameTime;
            if (timer.TimeRemaining <= 0)
            {
                RemComp<EmpTimerComponent>(timer.Owner);
                IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();
                _entityManager.EventBus.RaiseLocalEvent(timer.Owner, new EmpTimerEndEvent());
            }
        }
    }
}
