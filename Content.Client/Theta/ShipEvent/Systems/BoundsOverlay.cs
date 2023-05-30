using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class BoundsOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public Box2 Bounds;
    public MapId TargetMap;

    public BoundsOverlay()
    {
        IoCManager.InjectDependencies(this);
    }
    
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_entMan.TryGetComponent<TransformComponent>(_playerMan.LocalPlayer?.ControlledEntity, out var form))
        {
            if (form.MapID != TargetMap)
                return;
        }
        args.WorldHandle.DrawRect(Bounds.Enlarged(0.5f), Color.Red, false);
        args.WorldHandle.DrawRect(Bounds, Color.Red, false);
    }
}
