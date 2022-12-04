using Robust.Server.GameObjects;

namespace Content.Server.Ghost.Roles;

public sealed class UsePlayerNameForEntityNameSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ActorComponent, PlayerAttachedEvent>(ChangeEntityName);
    }

    private void ChangeEntityName(EntityUid uid, ActorComponent component, PlayerAttachedEvent args)
    {
        var metaDataComponent = EntityManager.GetComponent<MetaDataComponent>(args.Entity);
        metaDataComponent.EntityName = args.Player.Name;
    }
}
