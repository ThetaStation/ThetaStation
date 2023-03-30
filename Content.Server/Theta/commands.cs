using Content.Server.Administration;
using Content.Server.Theta.MobHUD;
using Content.Shared.Administration;
using Content.Shared.Theta.MobHUD;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Theta;

[AdminCommand(AdminFlags.Fun)]
public sealed class RedHUDCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;

    public string Command => "mhred";
    public string Description => "";
    public string Help => "";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var _hudSys = _entMan.System<MobHUDSystem>();
        
        if (shell.Player?.AttachedEntity == null)
            return;

        var player = (EntityUid)shell.Player.AttachedEntity;
        
        var hud = _entMan.EnsureComponent<MobHUDComponent>(player);
        var redHudProt = _protMan.Index<MobHUDPrototype>("ShipeventHUD");
        redHudProt.Color = "#ff0000";
        List<MobHUDPrototype> huds = new(){redHudProt};
        _hudSys.SetActiveHUDs(hud, huds);
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class BluHUDCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;

    public string Command => "mhblu";
    public string Description => "";
    public string Help => "";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var _hudSys = _entMan.System<MobHUDSystem>();
        
        if (shell.Player?.AttachedEntity == null)
            return;

        var player = (EntityUid)shell.Player.AttachedEntity;
        
        var hud = _entMan.EnsureComponent<MobHUDComponent>(player);
        var redHudProt = _protMan.Index<MobHUDPrototype>("ShipeventHUD");
        redHudProt.Color = "#0000ff";
        List<MobHUDPrototype> huds = new(){redHudProt};
        _hudSys.SetActiveHUDs(hud, huds);
    }
}

[AdminCommand(AdminFlags.Fun)]
public sealed class ClearHUDCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Command => "mhclr";
    public string Description => "";
    public string Help => "";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var _hudSys = _entMan.System<MobHUDSystem>();
        
        if (shell.Player?.AttachedEntity == null)
            return;

        var player = (EntityUid)shell.Player.AttachedEntity;
        
        var hud = _entMan.EnsureComponent<MobHUDComponent>(player);
        List<MobHUDPrototype> huds = new();
        _hudSys.SetActiveHUDs(hud, huds);
    }
}
