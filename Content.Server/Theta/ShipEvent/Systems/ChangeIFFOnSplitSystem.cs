using Content.Server.Shuttles.Systems;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class ChangeIFFOnSplitSystem : EntitySystem
{
    [Dependency] private readonly ShuttleSystem _shuttleSys = default!;
    [Dependency] private readonly TriggerSystem _triggerSys = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChangeIFFOnSplitComponent, PostGridSplitEvent>(OnSplit);
    }

    private void OnSplit(EntityUid uid, ChangeIFFOnSplitComponent comp, ref PostGridSplitEvent args)
    {
        if (comp.Replicate)
        {
            var newComp = AddComp<ChangeIFFOnSplitComponent>(args.Grid);
            (newComp.NewFlags, newComp.NewColor, newComp.Remove, newComp.Replicate, newComp.DeleteInheritedGridsDelay) =
                (comp.NewFlags, comp.NewColor, comp.Remove, comp.Replicate, comp.DeleteInheritedGridsDelay);
        }

        if (comp.Remove)
        {
            RemComp<IFFComponent>(args.Grid);
            return;
        }

        if (comp.DeleteInheritedGridsDelay > 0)
        {
            AddComp<DeleteOnTriggerComponent>(args.Grid).KeysIn = ["timer"];
            var timer = AddComp<TimerTriggerComponent>(args.Grid);
            timer.Delay = TimeSpan.FromSeconds(comp.DeleteInheritedGridsDelay);
            _triggerSys.ActivateTimerTrigger(args.Grid);
        }

        IFFComponent? originIff = CompOrNull<IFFComponent>(args.OldGrid);

        IFFFlags flags = comp.NewFlags ?? originIff?.Flags ?? IFFFlags.None;
        Color color = comp.NewColor ?? originIff?.Color ?? Color.Gold;

        var newIff = EnsureComp<IFFComponent>(args.Grid);
        _shuttleSys.AddIFFFlag(args.Grid, flags, newIff);
        _shuttleSys.SetIFFColor(args.Grid, color, newIff);
    }
}
