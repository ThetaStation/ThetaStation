using Content.Shared.Theta.MobHUD;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Theta.MobHUD;

public sealed class MobHUDOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly IEyeManager _eyeMan = default!;
    private readonly MobHUDSystem _hudSys;

    private Dictionary<SpriteSpecifier, Texture> cachedTextures = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MobHUDOverlay()
    {
        IoCManager.InjectDependencies(this);
        _hudSys = _entMan.System<MobHUDSystem>();
    }
    
    protected override void Draw(in OverlayDrawArgs args)
    {
        DrawingHandleWorld handle = args.WorldHandle;

        if (_hudSys.PlayerHUD == null)
            return;

        foreach ((var hud, var form) in _entMan.EntityQuery<MobHUDComponent, TransformComponent>())
        {
            foreach (var activeHud in hud.ActiveHUDs)
            {
                foreach (var allowedHud in activeHud.AllowedHUDs)
                {
                    var allowedHudPrototype = _protMan.Index<MobHUDPrototype>(allowedHud);

                    if (_hudSys.PlayerHUD.ActiveHUDs.Contains(allowedHudPrototype))
                    {
                        DrawHUD(form, activeHud, handle);
                        break;
                    }
                }
            }
        }

        handle.SetTransform(Matrix3.Identity);
    }

    public void DrawHUD(TransformComponent form, MobHUDPrototype hudProt, DrawingHandleWorld handle)
    {
        Texture texture = Texture.Transparent;

        if (!cachedTextures.ContainsKey(hudProt.Sprite))
        {
            switch (hudProt.Sprite)
            {
                case SpriteSpecifier.Texture tex:
                    if (_resCache.TryGetResource<TextureResource>(tex.TexturePath.ToRootedPath(), out var resource))
                        texture = resource.Texture;
                    break;
                case SpriteSpecifier.Rsi rsi:
                    var rsiRes = _resCache.GetResource<RSIResource>(rsi.RsiPath);
                    if (rsiRes.RSI.TryGetState(rsi.RsiState, out var state))
                        texture = state.Frame0;
                    break;
                default:
                    Logger.Warning($"{hudProt} sprite specifier is invalid, please check prototypes.");
                    break;
            }

            cachedTextures[hudProt.Sprite] = texture;
        }
        
        texture = cachedTextures[hudProt.Sprite];
        
        var angle = -_eyeMan.CurrentEye.Rotation;
        handle.SetTransform(form.WorldPosition, angle);
        
        var textureSize = texture.Size / (float)EyeManager.PixelsPerMeter;
        var rect = Box2.FromDimensions(textureSize/-2, textureSize);
        handle.DrawTextureRectRegion(texture, rect, hudProt.Color);
    }
}
