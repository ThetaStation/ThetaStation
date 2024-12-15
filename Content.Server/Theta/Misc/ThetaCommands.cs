using Content.Server.GameTicking;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Theta;

[AnyCommand]
sealed class RespawnCommand : IConsoleCommand
{
    public string Command => "respawnself";
    public string Description => "Respawns you.";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
            return;

        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GameTicker>().Respawn(shell.Player);
    }
}