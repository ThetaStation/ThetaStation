using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Server.Theta.AmmoContainer;

public sealed class AmmoContainerSystem : EntitySystem
{
    private const string AmmoExamineColor = "yellow";
    private const string BaseStorageId = "storagebase";

    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AmmoContainerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, AmmoContainerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if(_containerSystem.TryGetContainer(uid, BaseStorageId, out var container))
            args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", container.ContainedEntities.Count)));
    }
}
