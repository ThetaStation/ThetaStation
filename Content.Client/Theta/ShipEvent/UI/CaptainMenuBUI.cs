using System.Linq;
using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.UI;

[UsedImplicitly]
public sealed class CaptainMenuBoundUserInterface : BoundUserInterface
{
    private CaptainMenuWindow? _window;

    public CaptainMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {

        base.Open();

        _window = new CaptainMenuWindow();
        _window.OpenCentered();
        SendMessage(new ShipEventCaptainMenuRequestInfoMessage());
        _window.OnClose += Close;
        _window.ShipPickerButtonPressed += _ =>
        {
            SendMessage(new GetShipPickerInfoMessage());
        };
        _window.BlackListButtonPressed += _ =>
        {
            List<string> blacklist = _window.BlacklistText.Split(",").ToList();
            SendMessage(new ShipEventCaptainMenuChangeBlacklistMessage(blacklist));
        };
        _window.KickButtonPressed += _ =>
        {
            SendMessage(new ShipEventCaptainMenuKickMemberMessage(_window.KickCKey));
        };
        _window.shipPicker.OnSelectionMade += _ =>
        {
            if (_window.shipPicker.Selection == null)
                return;

            SendMessage(new ShipEventCaptainMenuChangeShipMessage(_window.shipPicker.Selection));
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case ShipEventCaptainMenuBoundUserInterfaceState msg:
                base.UpdateState(state);
                _window?.UpdateState(msg);
                break;
            case ShipPickerBoundUserInterfaceState pickerMsg:
                _window?.shipPicker.UpdateState(pickerMsg);
                break;
        }
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
