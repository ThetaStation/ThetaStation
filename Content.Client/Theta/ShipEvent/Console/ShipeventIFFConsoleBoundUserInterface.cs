using Content.Client.Theta.ShipEvent.UI;
using Content.Shared.Theta.ShipEvent.UI;
using Content.Shared.Shuttles.Events;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.Console;

[UsedImplicitly]
public sealed class ShipeventIFFConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ShipeventIFFConsoleWindow? _window;

    public ShipeventIFFConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new ShipeventIFFConsoleWindow();
        _window.OnClose += Close;
        _window.ShowVessel += SendVesselMessage;
        _window.OpenCenteredLeft();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ShipeventIFFConsoleBoundUserInterfaceState bState)
            return;

        _window?.UpdateState(bState);
    }

    private void SendVesselMessage(bool obj)
    {
        SendMessage(new IFFShowVesselMessage()
        {
            Show = obj,
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Close();
            _window = null;
        }
    }
}
