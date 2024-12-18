using System.ComponentModel;
using System.Linq;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Server.UserInterface;
using Content.Shared.Theta.ShipEvent.UI;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Server.Player;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class TeamConsoleSystem : EntitySystem
{
    [Dependency] private readonly ShipEventTeamSystem _shipSys = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TeamConsoleComponent, TeamCreationRequest>(OnTeamCreationRequest);
        SubscribeLocalEvent<TeamConsoleComponent, RefreshShipTeamsEvent>(OnRefreshTeams);
        SubscribeLocalEvent<TeamConsoleComponent, JoinToShipTeamsEvent>(TryJoinToShipTeam);
        SubscribeLocalEvent<TeamConsoleComponent, BeforeActivatableUIOpenEvent>(UpdateLobbyState);
    }

    private void TryJoinToShipTeam(EntityUid uid, TeamConsoleComponent component, JoinToShipTeamsEvent args)
    {
        var teams = _shipSys.Teams.Where(t => t.Name == args.Name);
        if (teams.Count() != 1)
            return;

        if(_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            _shipSys.JoinTeam(session, teams.First(), args.Password);
    }

    private void OnRefreshTeams(EntityUid uid, TeamConsoleComponent component, RefreshShipTeamsEvent args)
    {
        UpdateState(uid);
    }

    private void UpdateLobbyState(EntityUid uid, TeamConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateState(uid);
    }

    private void UpdateState(EntityUid uid)
    {
        _uiSystem.SetUiState(uid, TeamCreationUiKey.Key, new LobbyBoundUserInterfaceState(_shipSys.GetTeamStates()));
    }

    private void OnTeamCreationRequest(EntityUid uid, TeamConsoleComponent component, TeamCreationRequest args)
    {
        if (!_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            return;

        if (!_shipSys.AllowTeamRegistration)
        {
            SendResponse(uid, args.UiKey, ResponseTypes.TeamRegistrationDisabled);
        }

        if (!_shipSys.IsValidName(args.Name))
        {
            SendResponse(uid, args.UiKey, ResponseTypes.InvalidName);
            return;
        }

        _shipSys.CreateTeam(args.Name, args.ShipType, args.Password, args.MaxPlayers, session);
    }

    private void SendResponse(EntityUid uid, Enum uiKey, ResponseTypes response)
    {
        var text = "";
        switch (response)
        {
            case ResponseTypes.InvalidName:
                text = "shipevent-teamcreation-response-invalidname";
                break;
            case ResponseTypes.TeamRegistrationDisabled:
                text = "shipevent-teamcreation-response-regdisabled";
                break;
        }

        _uiSystem.SetUiState(uid, uiKey, new CreateTeamBoundUserInterfaceState(Loc.GetString(text)));
    }


    private enum ResponseTypes
    {
        InvalidName,
        TeamRegistrationDisabled
    }
}
