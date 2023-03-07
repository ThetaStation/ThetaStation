using Content.Shared.Theta.ShipEvent.UI;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.UI;

public sealed class TeamCreationBoundUserInterface : BoundUserInterface
{
    private TeamCreationWindow? _window;

    public TeamCreationBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new TeamCreationWindow();
        _window.OpenCentered();

        _window.OnClose += Close;
        _window.CreationButtonPressed += _ => { SendMessage(new TeamCreationRequest(_window._Name, _window._Blacklist, _window._Color)); };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.UpdateState((TeamCreationBoundUserInterfaceState)state);
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
