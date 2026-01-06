using Content.Shared.Theta.ShipEvent.UI;

namespace Content.Client.Theta.ShipEvent.UI;

public sealed class TeamViewBoundUserInterface : BoundUserInterface
{
    private TeamViewWindow? _window;

    public TeamViewBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new TeamViewWindow();
        _window.OpenCentered();
        _window.OnClose += Close;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        _window?.Update((TeamViewBoundUserInterfaceState) state);
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
