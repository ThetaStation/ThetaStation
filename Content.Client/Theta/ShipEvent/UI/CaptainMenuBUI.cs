using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;

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
        _window.OnClose += Close;
        _window.ShipPickerButtonPressed += _ =>
        {
            SendMessage(new GetShipPickerInfoMessage());
        };
        _window.KickButtonPressed += _ =>
        {
            SendMessage(new CaptainMenuKickMemberMessage(_window.KickCKey));
        };
        _window.ShipPicker.OnSelectionMade += _ =>
        {
            if (_window.ShipPicker.Selection == null)
                return;

            SendMessage(new CaptainMenuChangeShipMessage(_window.ShipPicker.Selection));
        };
        _window.SetMaxMembersButtonPressed += _ =>
        {
            SendMessage(new CaptainMenuSetMaxMembersMessage(_window.MaxMembers));
        };
        _window.SetPasswordButtonPressed += _ =>
        {
            SendMessage(new CaptainMenuSetPasswordMessage(_window.Password != "" ? _window.Password : null));
        };
        _window.SetCaptainButtonPressed += _ =>
        {
            SendMessage(new CaptainMenuSetCaptainMessage(_window.CaptainCKey != "" ? _window.CaptainCKey : null));
        };
        _window.FormFleetButtonPressed += _ =>
        {
            SendMessage(new CaptainMenuFormFleetMessage());
        };
        _window.RespawnTeamButtonPressed += _ =>
        {
            SendMessage(new CaptainMenuRespawnTeamMessage());
        };
        _window.DisbandTeamButtonPressed += _ =>
        {
            SendMessage(new CaptainMenuDisbandTeamMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case CaptainMenuBoundUserInterfaceState msg:
                base.UpdateState(state);
                _window?.UpdateState(msg);
                break;
            case ShipPickerBoundUserInterfaceState pickerMsg:
                _window?.ShipPicker.UpdateState(pickerMsg);
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
