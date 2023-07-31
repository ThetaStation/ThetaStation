using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.UI;

[UsedImplicitly]
public sealed class ShipEventLobbyBoundUserInterface : BoundUserInterface
{
    private TeamCreationWindow? _teamCreation;
    private EnterPasswordWindow? _enterPassword;
    private TeamLobbyWindow? _lobby;

    public ShipEventLobbyBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _teamCreation = new TeamCreationWindow();
        _lobby = new TeamLobbyWindow();
        _enterPassword = new EnterPasswordWindow();

        _lobby.OnClose += Close;
        _lobby.CreateTeamButtonPressed += _ =>
        {
            _teamCreation.OpenCentered();
        };
        _lobby.RefreshButtonPressed += _ =>
        {
            SendMessage(new RefreshShipTeamsEvent());
        };
        _lobby.JoinButtonPressed += (name, hasPassword) =>
        {
            if (hasPassword)
            {
                _enterPassword.SaveChosenTeamName(name);
                _enterPassword.OpenCentered();
            }
            else
            {
                SendMessage(new JoinToShipTeamsEvent(name, null));
            }
        };

        _teamCreation.CreationButtonPressed += _ =>
        {
            var password = _teamCreation.Password != "" ? _teamCreation.Password : null;
            SendMessage(new TeamCreationRequest(
                _teamCreation.TeamName,
                _teamCreation.TeamColor,
                _teamCreation.Blacklist,
                _teamCreation.ShipType,
                password,
                _teamCreation.MaxMembers));
        };

        _teamCreation.ShipPickerButtonPressed += _ =>
        {
            SendMessage(new GetShipPickerInfoMessage());
        };

        _enterPassword.EnterPasswordPressed += () =>
        {
            SendMessage(new JoinToShipTeamsEvent(_enterPassword.ChosenTeamName, _enterPassword.Password));
        };

        _lobby.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case ShipEventCreateTeamBoundUserInterfaceState msg:
                _teamCreation?.UpdateState(msg);
                break;
            case ShipPickerBoundUserInterfaceState msg:
                _teamCreation?.ShipPicker.UpdateState(msg);
                break;
            case ShipEventLobbyBoundUserInterfaceState msg:
                _lobby?.UpdateState(msg);
                break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _teamCreation?.Dispose();
            _lobby?.Dispose();
        }
    }
}
