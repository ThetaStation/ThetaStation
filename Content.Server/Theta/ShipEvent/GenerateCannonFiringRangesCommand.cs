using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Server.Theta.ShipEvent;

[AdminCommand(AdminFlags.Mapping)]
public sealed class GenerateCannonFiringRangesCommand : IConsoleCommand
{
    public string Command => "se_gencannonranges";
    public string Description => "Generates firing ranges for all cannons on specified grid.";
    public string Help => "Specify grid's entity uid.";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError("Please specify grid uid.");
            return;
        }

        if (EntityUid.TryParse(args[0], out EntityUid gridUid))
        {
            int count = 0;
            IEntityManager entMan = IoCManager.Resolve<IEntityManager>();
            CannonSystem cannonSys = entMan.System<CannonSystem>();
            
            Stopwatch watch = new Stopwatch();
            watch.Start();
            foreach ((CannonComponent cannon, TransformComponent form) in entMan.EntityQuery<CannonComponent, TransformComponent>())
            {
                if (form.ParentUid == gridUid)
                {
                    count++;
                    cannonSys.RefreshFiringRanges(cannon.Owner, cannon);
                }
            }

            shell.WriteLine($"Generated ranges for {count} cannons in {watch.Elapsed.TotalSeconds} seconds.");
        }
        else
        {
            shell.WriteError("Invalid grid uid.");
        }
    }
}
