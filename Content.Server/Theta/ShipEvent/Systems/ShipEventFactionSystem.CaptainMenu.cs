using Content.Shared.Chat;
using Content.Shared.Theta.ShipEvent.UI;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventFactionSystem
{
    private void InitializeCaptainMenu()
    {
        SubscribeAllEvent<ShipEventCaptainMenuChangeShipMessage>(OnShipChangeRequest);
        SubscribeAllEvent<ShipEventCaptainMenuKickMemberMessage>(OnKickMemberRequest);
        SubscribeAllEvent<ShipEventCaptainMenuSetPasswordMessage>(OnSetNewPassword);
        SubscribeAllEvent<ShipEventCaptainMenuSetMaxMembersMessage>(OnSetNewMaxMembers);
    }

    private void OnSetNewPassword(ShipEventCaptainMenuSetPasswordMessage msg)
    {
        foreach (var team in Teams)
        {
            if (team.Captain == msg.Session.ConnectedClient.UserName)
            {
                team.JoinPassword = msg.Password;
                break;
            }
        }
    }

    private void OnSetNewMaxMembers(ShipEventCaptainMenuSetMaxMembersMessage msg)
    {
        foreach (var team in Teams)
        {
            if (team.Captain == msg.Session.ConnectedClient.UserName)
            {
                team.MaxMembers = msg.MaxMembers;
                break;
            }
        }
    }

    private void OnShipChangeRequest(ShipEventCaptainMenuChangeShipMessage msg)
    {
        foreach (var team in Teams)
        {
            if (team.Captain == msg.Session.ConnectedClient.UserName)
            {
                team.ChosenShipType = msg.NewShip;
                var shipName = team.ChosenShipType.Name;
                TeamMessage(team, Loc.GetString("shipevent-team-ship-changed", ("name", Loc.GetString(shipName))),
                    color:  team.Color);
                break;
            }
        }
    }

    private void OnKickMemberRequest(ShipEventCaptainMenuKickMemberMessage msg)
    {
        foreach (var team in Teams)
        {
            if (team.Captain != msg.Session.ConnectedClient.UserName)
                continue;
            if (msg.CKey == team.Captain)
                break;
            var members = _factionSystem.GetMembersByUserNames(team);
            if (members.TryGetValue(msg.CKey, out var member))
            {
                var memberEntity = member.Owner;
                team.RemoveMember(member);
                EntityManager.DeleteEntity(memberEntity);
                if(_mindSystem.TryGetSession(member.Owner, out var session))
                    _chatSys.SendSimpleMessage(Loc.GetString("shipevent-kicked"), session, ChatChannel.Local, Color.DarkRed);
            }
            break;
        }
    }
}
