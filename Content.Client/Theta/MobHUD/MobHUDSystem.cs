using System.Linq;
using Content.Shared.Theta.MobHUD;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Client.Theta.MobHUD;

public sealed class MobHUDSystem : SharedMobHUDSystem
{
    [Dependency] private readonly IPrototypeManager protMan = default!;
    [Dependency] private readonly IClientGameStateManager statMan = default!;
    public MobHUDComponent PlayerHUD = default!;
    public Dictionary<MobHUDComponent, List<int>> UsedLayers = new();
    public HashSet<EntityUid> DetachedEntities = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobHUDComponent, ComponentInit>(OnHUDInit);
        SubscribeLocalEvent<MobHUDComponent, ComponentShutdown>(OnHUDShutdown);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetach);
        statMan.GameStateApplied += OnGameStateApplied;
    }

    private void OnGameStateApplied(GameStateAppliedArgs args)
    {
        foreach (EntityUid entity in args.Detached)
        {
            if (EntityManager.HasComponent<MobHUDComponent>(entity))
            {
                DetachedEntities.Add(entity);
            }
        }
        
        foreach (MobHUDComponent hud in EntityManager.EntityQuery<MobHUDComponent>())
        {
            var entity = hud.Owner;
            if (DetachedEntities.Contains(entity))
            {
                DetachedEntities.Remove(entity);
                UpdateSprite(EntityManager.GetComponent<SpriteComponent>(entity), hud);
            }
        }
    }

    public void OnPlayerAttach(PlayerAttachedEvent args)
    {
        PlayerHUD = default!;
        if (EntityManager.TryGetComponent<MobHUDComponent>(args.Entity, out var hud)) PlayerHUD = hud;
        UpdateAll();
    }

    public void OnPlayerDetach(PlayerDetachedEvent args)
    {
        PlayerHUD = default!;
        UpdateAll();
    }

    public void OnHUDInit(EntityUid entity, MobHUDComponent hud, ComponentInit args)
    {
        UsedLayers[hud] = new List<int>();
        UpdateSprite(EntityManager.GetComponent<SpriteComponent>(entity), hud);
    }

    public void OnHUDShutdown(EntityUid entity, MobHUDComponent hud, ComponentShutdown args)
    {
        if (hud == PlayerHUD)
        {
            PlayerHUD = default!;
            UpdateAll();
        }

        ResetSprite(EntityManager.GetComponent<SpriteComponent>(entity), hud);
        UsedLayers.Remove(hud);
    }

    public override void SetHUDState(EntityUid entity, MobHUDComponent hud, ref ComponentHandleState args)
    {
        base.SetHUDState(entity, hud, ref args);
        
        if(!UsedLayers.ContainsKey(hud)) UsedLayers[hud] = new List<int>();

        if (hud == PlayerHUD)
        {
            UpdateAll();
            return;
        }

        UpdateSprite(EntityManager.GetComponent<SpriteComponent>(entity), hud);
    }

    public void ResetSprite(SpriteComponent sprite, MobHUDComponent hud)
    {
        foreach (var layer in UsedLayers[hud])
        {
            sprite.RemoveLayer(layer);
        }

        UsedLayers[hud].Clear();
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

        //Since SpriteComponent stored layers as simple list and not as dict, removing layer will move all other layers above it to one position down.
        //Sorting is to prevent this case.
        UsedLayers[hud] = UsedLayers[hud].OrderDescending().ToList();
    }

    public void ApplyHUD(SpriteComponent sprite, MobHUDComponent hud, MobHUDPrototype hudPrototype)
    {
        var layerIndex = sprite.AddLayer(hudPrototype.Sprite);
        sprite.LayerSetColor(layerIndex, Color.FromHex(hudPrototype.Color));
        UsedLayers[hud].Add(layerIndex);
    }

    public void UpdateAll()
    {
        foreach (var (sprite, hud) in EntityQuery<SpriteComponent, MobHUDComponent>())
        {
            UpdateSprite(sprite, hud);
        }
    }
}
