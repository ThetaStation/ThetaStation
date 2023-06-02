using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class BoundsOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    private readonly TransformSystem _formSys;
    private ShaderInstance _monoColorShader;
    
    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public Box2 Bounds;
    public MapId TargetMap;

    public BoundsOverlay()
    {
        IoCManager.InjectDependencies(this);
        _formSys = _entMan.System<TransformSystem>();
        _monoColorShader = _protMan.Index<ShaderPrototype>("MonoColor").InstanceUnique();
    }
    
    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_entMan.TryGetComponent<TransformComponent>(_playerMan.LocalPlayer?.ControlledEntity, out var form))
        {
            if (form.MapID != TargetMap)
                return;

            if (!Bounds.Contains(_formSys.GetWorldPosition(form)))
            {
                if (ScreenTexture == null)
                    return;
                
                _monoColorShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
                _monoColorShader.SetParameter("UCOLOR", new Vector3(255,0,0));
                _monoColorShader.SetParameter("MIX", 0.1f);
                args.WorldHandle.UseShader(_monoColorShader);
                args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
            }
        }
        
        args.WorldHandle.DrawRect(Bounds.Enlarged(0.5f), Color.Red, false);
        args.WorldHandle.DrawRect(Bounds, Color.Red, false);
    }
}
