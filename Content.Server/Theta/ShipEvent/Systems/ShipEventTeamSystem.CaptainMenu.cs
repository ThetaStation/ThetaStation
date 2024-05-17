using Content.Shared.Chat;
using Content.Shared.Theta.ShipEvent.UI;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventTeamSystem
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
            if (team.Captain != msg.Session.Name)
                continue;

            if (msg.CKey == team.Captain)
                break;

            if (team.Members.Contains(msg.CKey))
            {
                team.Members.Remove(msg.CKey);
                if (_playerMan.TryGetSessionByUsername(msg.CKey, out var session))
                {
                    _chatSys.SendSimpleMessage(Loc.GetString("shipevent-kicked"), session, ChatChannel.Local, Color.DarkRed);
                    QueueDel(session.AttachedEntity);
                }
            }
            break;
        }
    }
}
