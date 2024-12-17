using Content.Shared.Theta.MobHUD;

namespace Content.Server.Theta.MobHUD;

public sealed class MobHUDSystem : SharedMobHUDSystem
{
    public void SetActiveHUDs(EntityUid uid, MobHUDComponent hud, List<MobHUDPrototype> activeHUDs)
    {
        hud.ActiveHUDs = activeHUDs;
        Dirty(uid, hud);
    }
}
