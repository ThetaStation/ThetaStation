using Content.Shared.Radiation.Components;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared.Radiation.Systems;

public sealed class RadiationPulseSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationPulseComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, RadiationPulseComponent component, ComponentStartup args)
    {
        component.StartTime = _timing.RealTime;

        // try to get radiation range or keep default visual range
        if (component.AutoRange && TryComp<RadiationSourceComponent>(uid, out var radSource))
        {
            component.VisualRange = radSource.Intensity / radSource.Slope;
        }
    }
}
