using Content.Shared.Theta.MobHUD;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Client.Theta.MobHUD;

public sealed class MobHUDSystem : SharedMobHUDSystem
{
    [Dependency] private readonly IPrototypeManager protMan = default!;
    [Dependency] private readonly IClientGameStateManager statMan = default!;
    [Dependency] private readonly IPlayerManager playerMan = default!;
    
    public HashSet<EntityUid> DetachedEntities = new();
    public MobHUDComponent PlayerHUD = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobHUDComponent, ComponentInit>(OnHUDInit);
        SubscribeLocalEvent<MobHUDComponent, ComponentShutdown>(OnHUDShutdown);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerEntityChange);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerEntityChange);
        statMan.GameStateApplied += OnGameStateApplied;
    }

    private void OnGameStateApplied(GameStateAppliedArgs args)
    {
        foreach (var entity in args.Detached)
        {
            if (EntityManager.HasComponent<MobHUDComponent>(entity))
                DetachedEntities.Add(entity);
        }

        foreach (var hud in EntityManager.EntityQuery<MobHUDComponent>())
        {
            var entity = hud.Owner;
            if (DetachedEntities.Contains(entity))
            {
                DetachedEntities.Remove(entity);
                UpdateSprite(EntityManager.GetComponent<SpriteComponent>(entity), hud);
            }
        }
    }

    public void OnPlayerEntityChange(PlayerAttachedEvent _)
    {
        UpdatePlayerHUD();
        UpdateAll();
    }
    
    public void OnPlayerEntityChange(PlayerDetachedEvent _)
    {
        UpdatePlayerHUD();
        UpdateAll();
    }

    public void OnHUDInit(EntityUid entity, MobHUDComponent hud, ComponentInit args)
    {
        UpdatePlayerHUD(); //if it's roundstart spawn and player haven't changed their entity yet
        
        if (hud == PlayerHUD)
        {
            UpdateAll();
            return;
        }
        
        UpdateSprite(EntityManager.GetComponent<SpriteComponent>(entity), hud);
    }

    public void OnHUDShutdown(EntityUid entity, MobHUDComponent hud, ComponentShutdown args)
    {
        if (hud == PlayerHUD)
        {
            PlayerHUD = default!;
            UpdateAll();
            return;
        }

        ResetSprite(EntityManager.GetComponent<SpriteComponent>(entity), hud);
    }

    public override void SetHUDState(EntityUid entity, MobHUDComponent hud, ref ComponentHandleState args)
    {
        base.SetHUDState(entity, hud, ref args);

        if (hud == PlayerHUD)
        {
            UpdateAll();
            return;
        }

        UpdateSprite(EntityManager.GetComponent<SpriteComponent>(entity), hud);
    }

    public void ResetSprite(SpriteComponent sprite, MobHUDComponent hud)
    {
        foreach (var mappedLayer in sprite.LayerMap)
        {
            if (mappedLayer.Key is string layerKey)
            {
                if(layerKey.Length < 4)
                    continue;
                
                if (layerKey.Substring(0, 4) == "HUD_")
                {
                    sprite.RemoveLayer(mappedLayer.Value);
                }
            }
        }
    }

    public void UpdateSprite(SpriteComponent sprite, MobHUDComponent hud)
    {
        ResetSprite(sprite, hud);
        if (PlayerHUD == null) return;

        foreach (var activeHud in hud.ActiveHUDs)
        {
            foreach (var allowedHud in activeHud.AllowedHUDs)
            {
                var allowedHudPrototype = protMan.Index<MobHUDPrototype>(allowedHud);
                if (PlayerHUD.ActiveHUDs.Contains(allowedHudPrototype))
                {
                    ApplyHUD(sprite, hud, activeHud);
                    break;
                }
            }
        }
    }

    public void ApplyHUD(SpriteComponent sprite, MobHUDComponent hud, MobHUDPrototype hudPrototype)
    {
        var index = sprite.AddLayer(hudPrototype.Sprite);
        sprite.LayerSetColor(index, Color.FromHex(hudPrototype.Color));
        var hudKey = GetHUDKey(hudPrototype);

        if (sprite.LayerMapTryGet(hudKey, out var oldIndex))
        {
            sprite.RemoveLayer(oldIndex);
            index--;
        }

        sprite.LayerMapSet(hudKey, index);
    }

    public void UpdateAll()
    {
        foreach (var (sprite, hud) in EntityQuery<SpriteComponent, MobHUDComponent>())
        {
            UpdateSprite(sprite, hud);
        }
    }

    public void UpdatePlayerHUD()
    {
        PlayerHUD = default!;

        var player = playerMan.LocalPlayer?.ControlledEntity;
        if (player == null)
            return;

        if (EntityManager.TryGetComponent<MobHUDComponent>(player, out var hud))
        {
            PlayerHUD = hud;
        }
    }

    public string GetHUDKey(MobHUDPrototype hud)
    {
        return "HUD_" + hud.ID;
    }
}
