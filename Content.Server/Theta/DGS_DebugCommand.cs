using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Theta;

[AdminCommand(AdminFlags.Debug)]
public sealed class DebugCommand : IConsoleCommand
{
    public string Command => "dgs_debug";
    public string Description => ".";
    public string Help => ".";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.ExecuteCommand("addgamerule ShipEvent");
        shell.ExecuteCommand("aghost");
        shell.ExecuteCommand("togglelight");
        shell.ExecuteCommand("rotateeyes 0");
        shell.ExecuteCommand("tp 0 0 2");
        shell.ExecuteCommand("zoom 20");
    }
}
