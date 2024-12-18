using Content.Shared.Theta.ShipEvent.UI;

namespace Content.Client.Theta.ShipEvent.UI;

public sealed class AdmiralMenuBoundUserInterface : BoundUserInterface
{
    private AdmiralMenuWindow? _window;

    public AdmiralMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = new AdmiralMenuWindow();
        _window.OpenCentered();
        _window.OnClose += Close;

        _window.TeamSelected += name => SendMessage(new AdmiralMenuManageTeamMessage { Name = name });
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is AdmiralMenuBoundUserInterfaceState admState)
            _window?.Update(admState);
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
