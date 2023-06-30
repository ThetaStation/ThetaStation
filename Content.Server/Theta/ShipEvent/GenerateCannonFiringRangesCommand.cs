using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Theta.ShipEvent;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.Theta.ShipEvent;

[AdminCommand(AdminFlags.Mapping)]
public sealed class GenerateCannonFiringRangesCommand : IConsoleCommand
{
    public string Command => "se_genranges";
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


[AdminCommand(AdminFlags.Mapping)]
public sealed class GenerateCannonFiringRangesCommand_Map : IConsoleCommand
{
    public string Command => "se_genranges_map";
    public string Description => "Generates firing ranges for all cannons on specified map (file).";
    public string Help => "Specify map's file path.";
    
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        IEntityManager entMan = IoCManager.Resolve<IEntityManager>();
        IMapManager mapMan = IoCManager.Resolve<IMapManager>();
        MapLoaderSystem mapLoader = entMan.System<MapLoaderSystem>();
        
        if (args.Length < 1)
        {
            shell.WriteError("Please specify map's file path.");
            return;
        }

        MapId liveMap = mapMan.CreateMap();
        MapId deadMap = mapMan.CreateMap();
        mapMan.AddUninitializedMap(deadMap);

        if (mapLoader.TryLoad(liveMap, args[0], out IReadOnlyList<EntityUid>? rootUidsLive) && 
            mapLoader.TryLoad(deadMap, args[0], out IReadOnlyList<EntityUid>? rootUidsDead))
        {
            int count = 0;
            
            EntityUid liveGridUid = rootUidsLive[0];
            EntityUid deadGridUid = rootUidsDead[0];
            MapGridComponent deadGrid = entMan.GetComponent<MapGridComponent>(deadGridUid);
            
            shell.ExecuteCommand("se_genranges " + liveGridUid);
            foreach ((CannonComponent cannon, TransformComponent form) in entMan.EntityQuery<CannonComponent, TransformComponent>())
            {
                if (form.ParentUid == liveGridUid)
                {
                    foreach (EntityUid uid in deadGrid.GetLocal(new EntityCoordinates(deadGridUid, form.Coordinates.Position)))
                    {
                        if (entMan.TryGetComponent<CannonComponent>(uid, out CannonComponent? deadCannon))
                        {
                            count++;
                            deadCannon.ObstructedRanges = cannon.ObstructedRanges;
                            break;
                        }
                    }
                }
            }
            
            mapLoader.Save(deadGridUid, "range_out.yml");
            shell.WriteLine("Copied ranges for " + count + " cannons. Output is saved to 'range_out.yml'.");
        }
        else
        {
            shell.WriteError("Failed to load map.");
        }
        
        mapMan.DeleteMap(liveMap);
        mapMan.DeleteMap(deadMap);
    }
}
