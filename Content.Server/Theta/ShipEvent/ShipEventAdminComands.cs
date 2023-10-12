using Content.Server.Administration;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using System.Linq;
using Content.Shared.Roles.Theta;

namespace Content.Server.Theta.ShipEvent;

//todo: make one compact panel for controlling all of this stuff

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

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleTeamRegistrationCommand : IConsoleCommand
{
    public string Command => "se_toggleteamreg";

    public string Description => "Disables/enables command registration.";

    public string Help => "";
    
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        ShipEventFactionSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventFactionSystem>();
        seSys.RemoveEmptyTeams = !seSys.RemoveEmptyTeams;
        shell.WriteLine("Empty team removal is now " + (seSys.RemoveEmptyTeams ? "enabled" : "disabled"));
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class ToggleEmptyTeamRemovalCommand : IConsoleCommand
{
    public string Command => "se_toggleteamrem";

    public string Description => "Disables/enables removal of empty teams.";

    public string Help => "";
    
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        ShipEventFactionSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventFactionSystem>();
        seSys.AllowTeamRegistration = !seSys.AllowTeamRegistration;
        shell.WriteLine("Team registration is now " + (seSys.AllowTeamRegistration ? "enabled" : "disabled"));
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class RemoveTeamCommand : LocalizedCommands
{
    public override string Command => "se_remteam";
    public override string Description => "Removes team with specified name. Additionally, removal reason can be specified";
    public override string Help => "First arg - team name, second arg (optional) - removal reason. " +
                                   "Name and removal reason should be separated by comma (as an argument itself), ex: 'se_remteam Team 1 , Assholes'";

    private ShipEventFactionSystem? seSys;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if(seSys == null)
            seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventFactionSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Please specify team name.");
            return;
        }

        string teamName = "";
        foreach (string arg in args)
        {
            if (arg == ",")
                break;
            teamName += arg + " ";
        }
        teamName = teamName.Trim();

        bool commaPassed = false;
        string removalReason = "";
        foreach (string arg in args)
        {
            if (commaPassed)
                removalReason += arg + " ";
            if (arg == ",")
                commaPassed = true;
        }

        seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventFactionSystem>();

        ShipEventFaction? teamToRemove = null;
        foreach (var team in seSys.Teams)
        {
            if (team.Name == teamName)
            {
                teamToRemove = team;
                break;
            }
        }

        if (teamToRemove == null)
        {
            shell.WriteError("No team with given name was found.");
            return;
        }
        seSys.RemoveTeam(teamToRemove, removalReason);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if(seSys == null)
            seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventFactionSystem>();
        if(args.Length == 1)
            return CompletionResult.FromOptions(seSys.Teams.Select(t => t.Name));
        return CompletionResult.Empty;
    }
}
