using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.UI;


public sealed class CircularShieldConsoleBoundUserInterface : BoundUserInterface
{
    private CircularShieldConsoleWindow? _window;
    
    public CircularShieldConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new CircularShieldConsoleWindow();
        _window.OpenCentered();
        _window.OnClose += Close;
        _window.OnParametersChanged += () => UpdateShieldParameters();
        
        SendMessage(new CircularShieldConsoleInfoRequest());
    }

    private void UpdateShieldParameters()
    {
        if (_window == null)
            return;

        SendMessage(new CircularShieldChangeParametersMessage(
            _window.Enabled, 
            Angle.FromDegrees(_window.Angle), 
            Angle.FromDegrees(_window.Width),
            _window.Radius));
    }
    
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (!(state is CircularShieldConsoleWindowBoundsUserInterfaceState shieldSt))
            return;
        
        base.UpdateState(state);
        _window?.UpdateState(shieldSt);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
