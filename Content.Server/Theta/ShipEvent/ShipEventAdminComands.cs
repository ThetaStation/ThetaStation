using Content.Server.Administration;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using System.Linq;
using Content.Shared.Roles.Theta;
using Robust.Server.Player;
using Robust.Shared.Player;

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
        ShipEventTeamSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();
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
            ShipEventTeamSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();
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
        ShipEventTeamSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();
        seSys.AllowTeamRegistration = !seSys.AllowTeamRegistration;
        shell.WriteLine("Team registration is now " + (seSys.AllowTeamRegistration ? "enabled" : "disabled"));
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
        ShipEventTeamSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();
        seSys.RemoveEmptyTeams = !seSys.RemoveEmptyTeams;
        shell.WriteLine("Empty team removal is now " + (seSys.RemoveEmptyTeams ? "enabled" : "disabled"));
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class RemoveTeamCommand : LocalizedCommands
{
    public override string Command => "se_remteam";
    public override string Description => "Removes team with specified name. Additionally, removal reason can be specified";
    public override string Help => "First arg - team name, second arg (optional) - removal reason. " +
                                   "Name and removal reason should be separated by comma, example: 'se_remteam Team 1 , Blah blah blah'";

    private ShipEventTeamSystem? _shipSys;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_shipSys == null)
            _shipSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();

        if (args.Length == 0)
        {
            shell.WriteError("Please specify team name.");
            return;
        }

        string teamName = "";
        int i = 0;
        for (; i < args.Length; i++)
        {
            if (args[i] == ",")
            {
                i++;
                break;
            }

            teamName += args[i] + " ";
        }
        teamName = teamName.Trim();

        string removalReason = "";
        for (; i < args.Length; i++)
        {
            removalReason += args[i] + " ";
        }

        ShipEventTeam? targetTeam = null;
        foreach (ShipEventTeam team in _shipSys.Teams)
        {
            if (team.Name == teamName)
            {
                targetTeam = team;
                break;
            }
        }

        if (targetTeam == null)
        {
            shell.WriteError("No team with given name was found.");
            return;
        }
        _shipSys.RemoveTeam(targetTeam, removalReason);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (_shipSys == null)
            _shipSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();

        if (args.Length == 1)
            return CompletionResult.FromOptions(_shipSys.Teams.Select(t => t.Name));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class DemoteCaptainCommand : LocalizedCommands
{
    public override string Command => "se_remcap";
    public override string Description => "Demotes captain of the team. New captain can be specified.";
    public override string Help => "First arg - team name, second arg (optional) - new captain's ckey." +
                                   "Name and new cap should be separated by comma, example: 'se_remcap Team 1 , Cappie'";

    private ShipEventTeamSystem? _shipSys;
    private IPlayerManager? _playerMan;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_shipSys == null || _playerMan == null)
        {
            _shipSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();
            _playerMan = IoCManager.Resolve<IPlayerManager>();
        }

        if (args.Length == 0)
        {
            shell.WriteError("Please specify team name.");
            return;
        }

        string teamName = "";
        int i = 0;
        for (; i < args.Length; i++)
        {
            if (args[i] == ",")
            {
                i++;
                break;
            }

            teamName += args[i] + " ";
        }
        teamName = teamName.Trim();

        string newCapName = args[i];

        ShipEventTeam? targetTeam = null;
        foreach (ShipEventTeam team in _shipSys.Teams)
        {
            if (team.Name == teamName)
            {
                targetTeam = team;
                break;
            }
        }

        if (targetTeam == null)
        {
            shell.WriteError("No team with given name was found.");
            return;
        }

        _playerMan.TryGetSessionByUsername(newCapName, out ICommonSession? newCap);
        _shipSys.AssignCaptain(targetTeam, newCap);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (_shipSys == null)
            _shipSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();

        if (args.Length == 1)
            return CompletionResult.FromOptions(_shipSys.Teams.Select(t => t.Name));

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class DemoteAdmiralCommand : LocalizedCommands
{
    public override string Command => "se_remadmiral";
    public override string Description => "Demotes admiral of the fleet. New admiral can be specified.";
    public override string Help => "First arg - team name, second arg (optional) - new admiral's ckey." +
                                   "Name and new cap should be separated by comma, example: 'se_remadm Team 1 , Admiral'";

    private ShipEventTeamSystem? _shipSys;
    private IPlayerManager? _playerMan;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_shipSys == null || _playerMan == null)
        {
            _shipSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();
            _playerMan = IoCManager.Resolve<IPlayerManager>();
        }

        if (args.Length == 0)
        {
            shell.WriteError("Please specify fleet name.");
            return;
        }

        string fleetName = "";
        int i = 0;
        for (; i < args.Length; i++)
        {
            if (args[i] == ",")
            {
                i++;
                break;
            }

            fleetName += args[i] + " ";
        }
        fleetName = fleetName.Trim();

        string newAdmiralName = args[i];

        ShipEventFleet? targetFleet = null;
        foreach (ShipEventFleet fleet in _shipSys.Fleets)
        {
            if (fleet.Name == fleetName)
            {
                targetFleet = fleet;
                break;
            }
        }

        if (targetFleet == null)
        {
            shell.WriteError("No team with given name was found.");
            return;
        }

        _playerMan.TryGetSessionByUsername(newAdmiralName, out ICommonSession? newAdmiral);
        _shipSys.AssignAdmiral(targetFleet, newAdmiral);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (_shipSys == null)
            _shipSys = IoCManager.Resolve<IEntityManager>().System<ShipEventTeamSystem>();

        if (args.Length == 1)
            return CompletionResult.FromOptions(_shipSys.Fleets.Select(t => t.Name));

        return CompletionResult.Empty;
    }
}
