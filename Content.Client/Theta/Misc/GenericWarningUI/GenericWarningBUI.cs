using Content.Shared.Theta.ShipEvent.Misc.GenericWarningUI;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.Misc.GenericWarningUI;

public sealed class GenericWarningWindowBoundUserInterface : BoundUserInterface
{
    private GenericWarningWindow? _window;

    public GenericWarningWindowBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new GenericWarningWindow();
        _window.OpenCentered();
        _window.OnClose += Close;

        _window.OnYesButtonPressed += _ => SendMessage(new GenericWarningYesPressedMessage());
        _window.OnNoButtonPressed += _ => SendMessage(new GenericWarningNoPressedMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.UpdateText((GenericWarningBoundUserInterfaceState)state);
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
