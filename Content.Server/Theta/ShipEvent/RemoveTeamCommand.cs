using System.Linq;
using Content.Server.Administration;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Theta.ShipEvent;

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
        foreach (ShipEventFaction team in seSys.Teams)
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
