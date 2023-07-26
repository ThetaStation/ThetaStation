using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class BoundsOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    private readonly TransformSystem _formSys;
    
    private ShaderInstance _boundsShader;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public DateTime LastTime;
    public float SecondsOutsideBounds;
    public const float FadeInTime = 1.5f;

    public Box2 Bounds;
    public MapId TargetMap;
    public const float BorderWidth = 0.5f;

    public BoundsOverlay()
    {
        IoCManager.InjectDependencies(this);
        _formSys = _entMan.System<TransformSystem>();
        _boundsShader = _protMan.Index<ShaderPrototype>("BoundsOverlay").InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_entMan.TryGetComponent<TransformComponent>(_playerMan.LocalPlayer?.ControlledEntity, out var form))
        {
            SecondsOutsideBounds += (float) (DateTime.Now - LastTime).TotalSeconds;

            if (form.MapID != TargetMap)
                return;

            if (!Bounds.Contains(_formSys.GetWorldPosition(form)))
            {
                if (ScreenTexture == null)
                    return;

                _boundsShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
                _boundsShader.SetParameter("BRIGHTNESS", Math.Clamp(SecondsOutsideBounds / FadeInTime, 0, 1));

                args.WorldHandle.UseShader(_boundsShader);
                args.WorldHandle.DrawRect(args.WorldBounds, Color.White);
                args.WorldHandle.UseShader(null);
            }
            else
            {
                SecondsOutsideBounds = 0;
            }
        }
        
        args.WorldHandle.DrawRect(new Box2(Bounds.Left - BorderWidth, Bounds.Bottom - BorderWidth, Bounds.Right - BorderWidth, Bounds.Top - BorderWidth), Color.Red, false);
        args.WorldHandle.DrawRect(Bounds, Color.Red, false);

        LastTime = DateTime.Now;
    }
}
