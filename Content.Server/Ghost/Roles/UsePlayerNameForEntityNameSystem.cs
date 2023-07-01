using Content.Server.Ghost.Roles.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Ghost.Roles;

public sealed class UsePlayerNameForEntityNameSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<UsePlayerNameForEntityNameComponent, PlayerAttachedEvent>(ChangeEntityName);
    }

    private void ChangeEntityName(EntityUid uid, UsePlayerNameForEntityNameComponent component, PlayerAttachedEvent args)
    {
        var metaDataComponent = EntityManager.GetComponent<MetaDataComponent>(args.Entity);
        metaDataComponent.EntityName = args.Player.Name;
    }
}
