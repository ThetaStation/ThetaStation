using System.Linq;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;

namespace Content.Server.Theta.ShipEvent.Console;

public sealed class TeamConsoleSystem : EntitySystem
{
    [Dependency] private readonly ShipEventFactionSystem _shipEventFaction = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TeamConsoleComponent, TeamCreationRequest>(OnTeamCreationRequest);
    }

    private void OnTeamCreationRequest(EntityUid uid, TeamConsoleComponent component, TeamCreationRequest args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        if (!_shipEventFaction.RuleSelected)
        {
            ThrowError(uid, args.UiKey, ErrorTypes.ShipEventNotStarted);
            return;
        }

        if (!_shipEventFaction.IsValidName(args.Name))
        {
            ThrowError(uid, args.UiKey, ErrorTypes.InvalidName);
            return;
        }

        var color = string.Empty;
        if (!string.IsNullOrEmpty(args.Color))
        {
            if (!_shipEventFaction.IsValidColor(args.Color))
            {
                ThrowError(uid, args.UiKey, ErrorTypes.InvalidColor);
                return;
            }

            color = args.Color;
        }
        else
            color = Color.White.ToHex();

        List<string> blacklist = new();
        if (!string.IsNullOrEmpty(args.Blacklist))
        {
            blacklist = args.Blacklist.Split(",").ToList();
            blacklist = blacklist.Select(ckey => ckey.Trim()).ToList();
        }

        if (blacklist.Contains(args.Session.ConnectedClient.UserName))
        {
            ThrowError(uid, args.UiKey, ErrorTypes.BlacklistedSelf);
            return;
        }

        _shipEventFaction.CreateShipEventTeam(args.Session, args.Name, color, blacklist);
    }

    private void ThrowError(EntityUid uid, Enum uiKey, ErrorTypes error)
    {
        var errorText = "";
        switch (error)
        {
            case ErrorTypes.InvalidName:
                errorText = "shipevent-teamcreation-response-invalidname";
                break;
            case ErrorTypes.InvalidColor:
                errorText = "shipevent-teamcreation-response-invalidcolor";
                break;
            case ErrorTypes.BlacklistedSelf:
                errorText = "shipevent-teamcreation-response-blacklistself";
                break;
            case ErrorTypes.ShipEventNotStarted:
                errorText = "shipevent-teamcreation-response-eventnotstarted";
                break;
        }

        _uiSystem.TrySetUiState(uid, uiKey,
            new TeamCreationBoundUserInterfaceState(
                Loc.GetString(errorText)
            )
        );
    }


    private enum ErrorTypes
    {
        InvalidName,
        InvalidColor,
        BlacklistedSelf,
        ShipEventNotStarted,
    }
}
