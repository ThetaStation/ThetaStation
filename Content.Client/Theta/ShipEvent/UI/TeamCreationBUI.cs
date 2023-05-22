using Content.Shared.Theta.ShipEvent.UI;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.UI;

[UsedImplicitly]
public sealed class ShipEventLobbyBoundUserInterface : BoundUserInterface
{
    private TeamCreationWindow? _teamCreation;
    private TeamLobbyWindow? _lobby;

    public ShipEventLobbyBoundUserInterface(ClientUserInterfaceComponent owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        _teamCreation = new TeamCreationWindow();
        _lobby = new TeamLobbyWindow();

        _lobby.OnClose += Close;
        _lobby.CreateTeamButtonPressed += _ =>
        {
            _teamCreation.OpenCentered();
        };
        _lobby.RefreshButtonPressed += _ =>
        {
            SendMessage(new RefreshShipTeamsEvent());
        };
        _lobby.JoinButtonPressed += name =>
        {
            SendMessage(new JoinToShipTeamsEvent(name));
        };
        
        _teamCreation.CreationButtonPressed += _ =>
        {
            SendMessage(new TeamCreationRequest(_teamCreation.TeamName, _teamCreation.TeamColor, _teamCreation.Blacklist, _teamCreation.ShipType));
        };
        _teamCreation.ShipPickerButtonPressed += _ =>
        {
            SendMessage(new GetShipPickerInfoMessage());
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
