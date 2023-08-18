using Content.Server.Administration;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Theta.ShipEvent;

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleRoundEndTimerCommand : IConsoleCommand
{
    public string Command => "se_toggleroundend";
    public string Description => "Disables/enables ship event timed round end.";
    public string Help => "";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        ShipEventFactionSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventFactionSystem>();
        seSys.TimedRoundEnd = !seSys.TimedRoundEnd;
        shell.WriteLine("Round end timer is now " + (seSys.TimedRoundEnd ? "enabled" : "disabled"));
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class SetRoundEndTimerCommand : IConsoleCommand
{
    public string Command => "se_setroundend";
    public string Description => "Sets ship event's round end timer value.";
    public string Help => "Specify new time in seconds. Notice that for round to end, timer should be equal or higher than round duration.";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Please, specify new time.");
            return;
        }

        if (int.TryParse(args[0], out int newTime))
        {
            ShipEventFactionSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventFactionSystem>();
            seSys.RoundendTimer = newTime;
            shell.WriteLine("Timer set successfully.");
        }
        else
        {
            shell.WriteError("Invalid time passed.");
        }
    }
}
