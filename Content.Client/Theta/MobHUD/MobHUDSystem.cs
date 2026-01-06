using Content.Shared.Theta.MobHUD;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Theta.MobHUD;

public sealed class MobHUDSystem : SharedMobHUDSystem
{
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    public MobHUDComponent? PlayerHUD;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobHUDComponent, ComponentInit>(OnHUDInit);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerEntityChange);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerEntityChange);
        _overlayMan.AddOverlay(new MobHUDOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<MobHUDOverlay>();
    }

    public void OnPlayerEntityChange(LocalPlayerAttachedEvent _)
    {
        UpdatePlayerHUD();
    }

    public void OnPlayerEntityChange(LocalPlayerDetachedEvent _)
    {
        UpdatePlayerHUD();
    }

    public void OnHUDInit(EntityUid entity, MobHUDComponent hud, ComponentInit args)
    {
        UpdatePlayerHUD();
    }

    public void UpdatePlayerHUD()
    {
        PlayerHUD = null;

        EntityUid? playerUid = _playerMan.LocalSession?.AttachedEntity;
        if (playerUid == null)
            return;

        if (EntityManager.TryGetComponent<MobHUDComponent>(playerUid, out var hud))
            PlayerHUD = hud;
    }
}
