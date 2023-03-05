using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Theta.MobHUD;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Theta.MobHUD;

public sealed class MobHUDSystem : SharedMobHUDSystem
{
    public void SetActiveHUDs(MobHUDComponent hud, List<MobHUDPrototype> activeHUDs)
    {
        hud.ActiveHUDs = activeHUDs;
        Dirty(hud);
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class AddTestHUD : IConsoleCommand
{
    [Dependency] private readonly IEntityManager entMan = default!;
    [Dependency] private readonly IPrototypeManager protMan = default!;

    public string Command => "testhud";
    public string Description => "Adds test HUD.";

    public string Help => "BLU as first arg to add blue hud, red otherwise.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var hudProt = protMan.Index<MobHUDPrototype>("TestHUD_RED");
        if (args.Length > 0)
        {
            if (args[0].ToLower() == "blu") hudProt = protMan.Index<MobHUDPrototype>("TestHUD_BLU");
        }
        var hud = entMan.EnsureComponent<MobHUDComponent>((EntityUid) shell.Player!.AttachedEntity!);
        var hudSys = entMan.EntitySysManager.GetEntitySystem<MobHUDSystem>();
        hudSys.SetActiveHUDs(hud, new List<MobHUDPrototype>{hudProt});
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class ClearHUDs : IConsoleCommand
{
    [Dependency] private readonly IEntityManager entMan = default!;
    [Dependency] private readonly IPrototypeManager protMan = default!;

    public string Command => "clearhud";
    public string Description => "Clears active HUDs.";

    public string Help => ".";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var hudSys = entMan.EntitySysManager.GetEntitySystem<MobHUDSystem>();
        var hud = entMan.EnsureComponent<MobHUDComponent>((EntityUid) shell.Player!.AttachedEntity!);
        hudSys.SetActiveHUDs(hud, new List<MobHUDPrototype>());
    }
}