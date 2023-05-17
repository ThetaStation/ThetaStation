using Content.Shared.Theta.ShipEvent.Console;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.Console;

[UsedImplicitly]
public sealed class CannonConsoleBoundUserInterface : BoundUserInterface
{
    private CannonConsoleWindow? _window;

    public CannonConsoleBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _window = new CannonConsoleWindow();
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not CannonConsoleBoundInterfaceState cState)
            return;

        _window?.SetMatrix(cState.Coordinates, cState.Angle);
        _window?.UpdateState(cState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
