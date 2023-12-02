using Content.Shared.Theta.MobHUD;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Theta.MobHUD;

public sealed class MobHUDSystem : SharedMobHUDSystem
{
    [Dependency] private readonly IPrototypeManager protMan = default!;
    [Dependency] private readonly IClientGameStateManager statMan = default!;
    [Dependency] private readonly IPlayerManager playerMan = default!;
    [Dependency] private readonly IOverlayManager overlayMan = default!;

    public MobHUDComponent? PlayerHUD;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobHUDComponent, ComponentInit>(OnHUDInit);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerEntityChange);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerEntityChange);
        overlayMan.AddOverlay(new MobHUDOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        overlayMan.RemoveOverlay<MobHUDOverlay>();
    }

    public void OnPlayerEntityChange(PlayerAttachedEvent _)
    {
        UpdatePlayerHUD();
    }

    public void OnPlayerEntityChange(PlayerDetachedEvent _)
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

        var player = playerMan.LocalPlayer?.ControlledEntity;
        if (player == null)
            return;

        if (EntityManager.TryGetComponent<MobHUDComponent>(player, out var hud))
            PlayerHUD = hud;
    }
}
