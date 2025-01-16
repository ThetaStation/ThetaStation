using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Client.Theta.ShipEvent.Console;


[UsedImplicitly]
public sealed class CircularShieldConsoleBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private CircularShieldConsoleWindow? _window;

    // Smooth changing the shield parameters causes a spam to server
    private TimeSpan _updateCd = TimeSpan.FromMilliseconds(1);
    private TimeSpan _nextUpdate;

    public CircularShieldConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new CircularShieldConsoleWindow();
        _window.OpenCentered();
        _window.OnClose += Close;
        _window.OnEnableButtonPressed += () => SendMessage(new CircularShieldToggleMessage());
        _window.OnShieldParametersChanged += UpdateShieldParameters;
    }

    private void UpdateShieldParameters(Angle? angle, Angle? width, int? radius)
    {
        if (_nextUpdate > _gameTiming.RealTime)
            return;
        _nextUpdate = _gameTiming.RealTime + _updateCd;

        SendMessage(new CircularShieldChangeParametersMessage(angle, width, radius));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not ShieldConsoleBoundsUserInterfaceState shieldState)
            return;

        _window?.SetMatrix(EntMan.GetCoordinates(shieldState.NavState.Coordinates), shieldState.NavState.Angle);
        _window?.SetOwner(Owner);
        _window?.UpdateState(shieldState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}
