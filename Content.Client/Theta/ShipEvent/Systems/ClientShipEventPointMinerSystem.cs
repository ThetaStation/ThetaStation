using Content.Shared.Theta.ShipEvent.Misc;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class ClientPointMinerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<ShipEventPointMinerSetColorMessage>(OnColorSet);
    }

    private void OnColorSet(ShipEventPointMinerSetColorMessage msg)
    {
        EntityUid uid = GetEntity(msg.NetUid);

        if (!EntityManager.TryGetComponent(uid, out SpriteComponent? sprite))
            return;

        sprite.LayerSetColor(sprite.LayerMapGet("computerLayerScreen"), msg.Color);
    }
}
