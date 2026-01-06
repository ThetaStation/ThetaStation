using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.ShipEvent;

namespace Content.Client.Theta.ShipEvent.Console;

public sealed class RocketConsoleBoundUserInterface : BoundUserInterface
{
    private RocketConsoleWindow? _window;

    public RocketConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();
        _window = new RocketConsoleWindow();
        _window.OnClose += Close;
        _window.OnLaunchButtonPressed += () => SendMessage(new RocketConsoleLaunchMessage() { Waypoints = _window.RadarModule.Waypoints });
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not RadarConsoleBoundInterfaceState radarState)
            return;

        _window?.SetMatrix(EntMan.GetCoordinates(radarState.NavState.Coordinates), radarState.NavState.Angle);
        _window?.SetOwner(Owner);
        _window?.UpdateState(radarState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
