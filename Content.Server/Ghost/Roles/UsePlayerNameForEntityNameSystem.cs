using Content.Server.Ghost.Roles.Components;
using Robust.Shared.Player;

namespace Content.Server.Ghost.Roles;

public sealed class UsePlayerNameForEntityNameSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaDataSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<UsePlayerNameForEntityNameComponent, PlayerAttachedEvent>(ChangeEntityName);
    }

    private void ChangeEntityName(EntityUid uid, UsePlayerNameForEntityNameComponent component, PlayerAttachedEvent args)
    {
        if(component.Applied)
            return;
        var metaDataComponent = EntityManager.GetComponent<MetaDataComponent>(args.Entity);
        _metaDataSystem.SetEntityName(args.Entity, args.Player.Name);
        component.Applied = true;
    }
}
