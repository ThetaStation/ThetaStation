using Robust.Shared.GameStates;

namespace Content.Shared.Theta.MobHUD;

public abstract class SharedMobHUDSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobHUDComponent, ComponentInit>(OnHUDInit);
        SubscribeLocalEvent<MobHUDComponent, ComponentShutdown>(OnHUDShutdown);
        SubscribeLocalEvent<MobHUDComponent, ComponentGetState>(GetHUDState);
        SubscribeLocalEvent<MobHUDComponent, ComponentHandleState>(SetHUDState);
    }

    public virtual void OnHUDInit(EntityUid entity, MobHUDComponent hud, ComponentInit args) { }

    public virtual void OnHUDShutdown(EntityUid entity, MobHUDComponent hud, ComponentShutdown args) { }

    public void GetHUDState(EntityUid entity, MobHUDComponent hud, ref ComponentGetState args)
    {
        args.State = new MobHUDState
        {
            ActiveHUDs = hud.ActiveHUDs
        };
    }

    public virtual void SetHUDState(EntityUid entity, MobHUDComponent hud, ref ComponentHandleState args)
    {
        if (args.Current is not MobHUDState state) return;
        hud.ActiveHUDs = state.ActiveHUDs;
    }
}
