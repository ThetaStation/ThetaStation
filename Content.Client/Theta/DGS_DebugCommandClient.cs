using Robust.Shared.Console;

namespace Content.Client.Theta;

public sealed class DGS_DebugCommandClient : IConsoleCommand
{
    public string Command => "dgs_debug";
    public string Description => ".";
    public string Help => ".";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.RemoteExecuteCommand("addgamerule ShipEvent");
        shell.RemoteExecuteCommand("aghost");
        shell.RemoteExecuteCommand("tp 0 0 2");
        shell.ExecuteCommand("togglelight");
        shell.ExecuteCommand("rotateeyes 0");
        shell.ExecuteCommand("zoom 10");
    }
}
