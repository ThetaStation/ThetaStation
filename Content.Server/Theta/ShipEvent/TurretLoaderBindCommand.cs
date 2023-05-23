using Content.Server.Administration;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Administration;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Console;

namespace Content.Server.Theta.ShipEvent;


[AdminCommand(AdminFlags.Debug)]
public sealed class TurretLoaderBindCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    public string Command => "se_bindloader";
    public string Description => "Binds loader to turret.";
    public string Help => "First arg - loader entity, second arg - turret entity.";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            Logger.Error("Command requires exactly two arguments.");
            return;
        }

        EntityUid loaderEnt, turretEnt;
        
        try
        {
            loaderEnt = new EntityUid(int.Parse(args[0]));
            turretEnt = new EntityUid(int.Parse(args[1]));
        }
        catch(FormatException)
        {
            Logger.Error("Given arguments are not integers (entity uids).");
            return;
        }

        if (!(loaderEnt.IsValid() && turretEnt.IsValid()))
        {
            Logger.Error("Given entity uids are not valid.");
            return;
        }

        var loaderSys = _entMan.SystemOrNull<TurretLoaderSystem>();
        if (loaderSys == null)
        {
            Logger.Error("TurretLoaderSystem not found.");
            return;
        }

        if (_entMan.TryGetComponent<TurretLoaderComponent>(loaderEnt, out var loader))
        {
            loader.BoundTurret = turretEnt;
            loaderSys.SetupLoader(loaderEnt, loader);
            return;
        }
        Logger.Error("Loader does not have TurretLoaderComponent.");
    }
}
