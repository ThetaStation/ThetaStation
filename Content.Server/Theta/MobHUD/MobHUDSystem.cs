using Content.Shared.Theta.MobHUD;

namespace Content.Server.Theta.MobHUD;

public sealed class MobHUDSystem : SharedMobHUDSystem
{
    public void SetActiveHUDs(MobHUDComponent hud, List<MobHUDPrototype> activeHUDs)
    {
        hud.ActiveHUDs = activeHUDs;
        Dirty(hud);
    }
}
