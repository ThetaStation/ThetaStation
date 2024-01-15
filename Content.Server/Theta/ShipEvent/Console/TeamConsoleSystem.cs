using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Server.UserInterface;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class TeamConsoleSystem : EntitySystem
{
    [Dependency] private readonly ShipEventFactionSystem _shipSys = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;

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

        _shipSys.JoinTeam(args.Session, teams.First(), args.Password);
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
        _uiSystem.TrySetUiState(uid, TeamCreationUiKey.Key, new ShipEventLobbyBoundUserInterfaceState(GetTeams()));
    }

    private List<ShipTeamForLobbyState> GetTeams()
    {
        List<ShipTeamForLobbyState> teamStates = new();
        foreach (var team in _shipSys.Teams)
        {
            var hasPassword = team.JoinPassword != null;
            teamStates.Add(new ShipTeamForLobbyState(team.Name, team.Members.Count, team.Captain, hasPassword, team.MaxMembers));
        }

        return teamStates;
    }

    private void OnTeamCreationRequest(EntityUid uid, TeamConsoleComponent component, TeamCreationRequest args)
    {
        if (args.Session.AttachedEntity == null)
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

        _shipSys.CreateTeam(args.Session, args.Name, args.ShipType, args.Password, args.MaxPlayers);
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

        _uiSystem.TrySetUiState(uid, uiKey, new ShipEventCreateTeamBoundUserInterfaceState(Loc.GetString(text)));
    }


    private enum ResponseTypes
    {
        InvalidName,
        TeamRegistrationDisabled
    }
}
