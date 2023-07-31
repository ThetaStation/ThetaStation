using Content.Shared.Chat;
using Content.Shared.Theta.ShipEvent.UI;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventFactionSystem
{
    private void InitializeCaptainMenu()
    {
        SubscribeAllEvent<ShipEventCaptainMenuRequestInfoMessage>(OnCapMenuInfoRequest);
        SubscribeAllEvent<ShipEventCaptainMenuChangeShipMessage>(OnShipChangeRequest);
        SubscribeAllEvent<ShipEventCaptainMenuChangeBlacklistMessage>(OnBlacklistChangeRequest);
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

    private void OnBlacklistChangeRequest(ShipEventCaptainMenuChangeBlacklistMessage msg)
    {
        foreach (var team in Teams)
        {
            if (team.Captain == msg.Session.ConnectedClient.UserName)
            {
                team.Blacklist = msg.NewBlacklist;
                break;
            }
        }
    }

    private void OnKickMemberRequest(ShipEventCaptainMenuKickMemberMessage msg)
    {
        foreach (var team in Teams)
        {
            if (team.Captain == msg.Session.ConnectedClient.UserName)
            {
                var members = team.GetMembersByUserNames();
                if (members.ContainsKey(msg.CKey))
                {
                    var member = members[msg.CKey];
                    var memberEntity = team.GetMemberEntity(member);
                    team.RemoveMember(member);
                    EntityManager.DeleteEntity(memberEntity);

                    var session = team.GetMemberSession(member);
                    if(session != null)
                        _chatSys.SendSimpleMessage(Loc.GetString("shipevent-kicked"), session, ChatChannel.Local, Color.DarkRed);
                }
                break;
            }
        }
    }
}
