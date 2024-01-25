using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;

namespace Content.Client.Theta.ShipEvent.Console;

//todo (radars): move this into some kind of module/force it to use client comps (we are already ignoring pvs for controlled cannons)
[UsedImplicitly]
public sealed class CannonConsoleBoundUserInterface : BoundUserInterface
{
    private CannonConsoleWindow? _window;

    public CannonConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _window = new CannonConsoleWindow();
        _window.OnClose += Close;
        _window.OpenCentered();

        var msg = new CannonConsoleBUIStateMessage();
        msg.Created = true;
        SendMessage(msg);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CannonConsoleBoundInterfaceState cState)
            return;

        _window?.SetMatrix(EntMan.GetCoordinates(cState.Coordinates), cState.Angle);
        _window?.SetOwner(Owner);
        _window?.UpdateState(cState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            var msg = new CannonConsoleBUIStateMessage();
            SendMessage(msg);

            _window?.Dispose();
        }
    }
}
