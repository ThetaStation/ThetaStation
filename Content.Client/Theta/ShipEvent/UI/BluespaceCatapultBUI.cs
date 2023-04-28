using Content.Shared.Theta.ShipEvent.UI;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.UI;

public sealed class BluespaceCatapultBoundUserInterface : BoundUserInterface
{
    private BluespaceCatapultWindow? _window;
    
    public BluespaceCatapultBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        
        _window = new BluespaceCatapultWindow();
        _window.OpenCentered();
        _window.OnClose += Close;
        _window.LaunchButtonPressed += _ => { SendMessage(new BluespaceCatapultLaunchRequest(_window.Elevation, _window.Bearing, _window.Power)); };
        _window.RefreshButtonPressed += _ => { SendMessage(new BluespaceCatapultRefreshRequest()); };

        SendMessage(new BluespaceCatapultRefreshRequest());
    }
    
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not BluespaceCatapultBoundUserInterfaceState loaderState)
            return;
        
        base.UpdateState(state);
        _window?.UpdateState(loaderState);
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
