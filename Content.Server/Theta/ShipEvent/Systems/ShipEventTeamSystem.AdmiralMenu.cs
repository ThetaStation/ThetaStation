using Content.Shared.Roles.Theta;
using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Shared.Player;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventTeamSystem
{
    private void InitializeAdmiralMenu()
    {
        SubscribeAllEvent<AdmiralMenuManageTeamMessage>(OnManageTeamRequest);
        SubscribeAllEvent<AdmiralMenuCreateTeamMessage>(OnCreateTeamRequest);
    }

    private void OnManageTeamRequest(AdmiralMenuManageTeamMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out ICommonSession? session))
            return;

        Enum uiKey = CaptainMenuUiKey.Key;
        if (_uiSys.IsUiOpen(msg.Actor, uiKey))
            return;

        ShipEventFleet? targetFleet = null;
        if (TryComp<ShipEventTeamMarkerComponent>(msg.Actor, out var marker))
            targetFleet = marker.Team?.Fleet;

        if (targetFleet == null)
            return;

        ShipEventTeam? targetTeam = null;
        foreach (ShipEventTeam team in Teams)
        {
            if (team.Name == msg.Name)
            {
                targetTeam = team;
                break;
            }
        }

        if (targetTeam == null)
            return;

        targetFleet.ManagedByAdmiral = targetTeam;

        _uiSys.SetUiState(msg.Actor, uiKey, new CaptainMenuBoundUserInterfaceState(
            targetTeam.Name,
            targetTeam.Members,
            targetTeam.ChosenShipType,
            targetTeam.JoinPassword,
            targetTeam.MaxMembers));
        _uiSys.OpenUi(msg.Actor, uiKey, session);
    }

    private void OnCreateTeamRequest(AdmiralMenuCreateTeamMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out ICommonSession? session))
            return;

        ShipEventFleet? targetFleet = null;
        if (TryComp<ShipEventTeamMarkerComponent>(msg.Actor, out var marker))
            targetFleet = marker.Team?.Fleet;

        if (targetFleet == null)
            return;

        if (!TryCreateTeam(msg.Name, targetFleet.Color, null, null, 0, null, out var team) || !TryAddTeamToFleet(team, targetFleet))
        {
            //todo: this should be inside admiral's menu but I'm tired so someone else do it
            //or better yet, integrate team creation window from lobby console into it
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-admmenu-createfailed"), session, color: Color.DarkRed);

            if (team != null)
                RemoveTeam(team);
        }

        //refresh team list
        _uiSys.SetUiState(msg.Actor, msg.UiKey, new AdmiralMenuBoundUserInterfaceState
        {
            Teams = GetTeamStates(targetFleet)
        });
    }
}
