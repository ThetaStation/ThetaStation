using Content.Shared.Roles.Theta;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Shared.Player;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventTeamSystem
{
    private void InitializeAdmiralMenu()
    {
        SubscribeAllEvent<AdmiralMenuManageTeamMessage>(OnManageTeamRequest);
    }

    private void OnManageTeamRequest(AdmiralMenuManageTeamMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out ICommonSession? session))
            return;

        Enum uiKey = CaptainMenuUiKey.Key;
        if (_uiSys.IsUiOpen(msg.Actor, uiKey))
            return;

        ShipEventFleet? targetFleet = null;
        foreach (ShipEventFleet fleet in Fleets)
        {
            if (fleet.Admiral == session.Channel.UserName)
            {
                targetFleet = fleet;
                break;
            }
        }

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
            targetTeam.Members,
            targetTeam.ChosenShipType,
            targetTeam.JoinPassword,
            targetTeam.MaxMembers));
        _uiSys.OpenUi(msg.Actor, uiKey, session);
    }
}
