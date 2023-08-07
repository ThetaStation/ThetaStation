using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.Console;


[UsedImplicitly]
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
        _window.OnEnableButtonPressed += () => SendMessage(new CircularShieldToggleMessage());
        _window.OnParametersChanged += UpdateShieldParameters;
    }

    private void UpdateShieldParameters(int angle, int shieldWidth, int radius)
    {
        SendMessage(new CircularShieldChangeParametersMessage(
            Angle.FromDegrees(angle),
            Angle.FromDegrees(shieldWidth),
            radius));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not ShieldConsoleBoundsUserInterfaceState shieldSt)
            return;

        _window?.SetMatrix(shieldSt.Coordinates, shieldSt.Angle);
        _window?.SetOwner(Owner);
        _window?.UpdateState(shieldSt);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
