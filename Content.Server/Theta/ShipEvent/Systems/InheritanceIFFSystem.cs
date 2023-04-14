using Content.Server.Shuttles.Systems;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Shuttles.Components;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class InheritanceIFFSystem : EntitySystem
{
    [Dependency] private readonly ShuttleSystem _shuttleSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InheritanceIFFComponent, PostGridSplitEvent>(OnSplitGrid);
    }

    private void OnSplitGrid(EntityUid uid, InheritanceIFFComponent component, ref PostGridSplitEvent args)
    {
        if(!TryComp<IFFComponent>(args.OldGrid, out var originIff))
            return;

        AddComp<InheritanceIFFComponent>(args.Grid);

        var newIff = EnsureComp<IFFComponent>(args.Grid);
        _shuttleSystem.AddIFFFlag(args.Grid, originIff.Flags, newIff);
        _shuttleSystem.SetIFFColor(args.Grid, originIff.Color, newIff);
    }
}
