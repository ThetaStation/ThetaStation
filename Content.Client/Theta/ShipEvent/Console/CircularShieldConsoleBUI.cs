using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.Theta.ShipEvent.Console;


[UsedImplicitly]
public sealed class CircularShieldConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private CircularShieldConsoleWindow? _window;

    // Smooth changing the shield parameters causes a spam to server
    private TimeSpan _updateCd = TimeSpan.FromMilliseconds(1);
    private TimeSpan _nextCanUpdate;

    public CircularShieldConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new CircularShieldConsoleWindow();
        _window.OpenCentered();
        _window.OnClose += Close;
        _window.OnEnableButtonPressed += () => SendMessage(new CircularShieldToggleMessage());
        _window.OnAngleChanged += UpdateShieldParameters;
    }

    private void UpdateShieldParameters(Angle angle)
    {
        if(_nextCanUpdate > _gameTiming.RealTime)
            return;
        _nextCanUpdate = _gameTiming.RealTime + _updateCd;

        SendMessage(new CircularShieldChangeParametersMessage(angle));
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
