using Content.Server.Administration;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Theta.ShipEvent;

[AdminCommand(AdminFlags.Admin)]
public sealed class RemoveTeamCommand : IConsoleCommand
{
    public string Command => "se_remteam";
    public string Description => "Removes team with specified name. Additionally, removal reason can be specified";
    public string Help => "First arg - team name, second arg (optional) - removal reason";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError("Please specify team name.");
            return;
        }

        ShipEventFactionSystem seSys = IoCManager.Resolve<IEntityManager>().System<ShipEventFactionSystem>();
        
        ShipEventFaction? teamToRemove = null;
        foreach (ShipEventFaction team in seSys.Teams)
        {
            if (team.Name == args[0])
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
        seSys.RemoveTeam(teamToRemove, args.Length == 2 ? args[1] : "");
    }
}
