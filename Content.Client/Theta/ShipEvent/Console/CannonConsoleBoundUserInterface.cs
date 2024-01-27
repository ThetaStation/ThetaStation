using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;

namespace Content.Client.Theta.ShipEvent.Console;

//todo (radars): move this into some kind of module
[UsedImplicitly]
public sealed class CannonConsoleBoundUserInterface : BoundUserInterface
{
    private CannonConsoleWindow? _window;

    private CannonSystem _cannonSys = default!;

    public CannonConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _window = new CannonConsoleWindow();
        _window.OnClose += Close;
        _window.OpenCentered();

        _cannonSys = EntMan.System<CannonSystem>();

        SendMessage(new CannonConsoleBUICreatedMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not RadarConsoleBoundInterfaceState radarState)
            return;

        _window?.SetMatrix(EntMan.GetCoordinates(radarState.Coordinates), radarState.Angle);
        _window?.SetOwner(Owner);

        var ammoValues = new List<(int, int)>();
        var query = EntMan.EntityQueryEnumerator<CannonComponent>();
        while (query.MoveNext(out var uid, out var cannon))
        {
            if (cannon.BoundConsoleUid == Owner)
                ammoValues.Add(_cannonSys.GetCannonAmmoCount(uid, cannon));
        }

        _window?.UpdateState(radarState, ammoValues);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            SendMessage(new CannonConsoleBUIDisposedMessage());
            _window?.Dispose();
        }
    }
}
