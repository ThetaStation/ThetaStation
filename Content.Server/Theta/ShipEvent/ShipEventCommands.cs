using Content.Server.Administration;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Theta.ShipEvent;

[AdminCommand(AdminFlags.Debug)]
public sealed class ShipEventCreateTeamCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    private ShipEventFactionSystem _shipSys = default!;

    public string Command => "se_createteam";
    public string Description => "Creates new team.";

    public string Help => "Name (optional, will create new if not specified); " +
                          "Ship uid (optional, will create new if not specified); " +
                          "Captain uid (optional, will select YOU as captain if not specified)";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_shipSys == null) { _shipSys = _entMan.EntitySysManager.GetEntitySystem<ShipEventFactionSystem>(); }

        string name;
        EntityUid ship;
        EntityUid captain;

        if (args.Length > 3) { shell.WriteError(Loc.GetString("shell-wrong-arguments-number")); return; }

        name = args.Length > 0 ? args[0] : "";
        ship = args.Length > 1 ? EntityUid.Parse(args[1]) : _shipSys.CreateShip();
        captain = args.Length > 2 ? EntityUid.Parse(args[2]) : shell.Player!.AttachedEntity ?? EntityUid.Invalid;

        if (args.Length > 1) { ship = EntityUid.Parse(args[1]); }
        if (args.Length > 2) { captain = EntityUid.Parse(args[2]); }
        _shipSys.CreateTeam(ship, captain, name);
    }
}
