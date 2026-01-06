using Content.Shared.Chat;
using Content.Shared.Roles.Theta;
using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Shared.Player;

namespace Content.Server.Theta.ShipEvent.Systems;

public partial class ShipEventTeamSystem
{
    private void InitializeCaptainMenu()
    {
        SubscribeAllEvent<CaptainMenuChangeShipMessage>(OnShipChangeRequest);
        SubscribeAllEvent<CaptainMenuKickMemberMessage>(OnKickMemberRequest);
        SubscribeAllEvent<CaptainMenuSetPasswordMessage>(OnSetNewPassword);
        SubscribeAllEvent<CaptainMenuSetMaxMembersMessage>(OnSetNewMaxMembers);
        SubscribeAllEvent<CaptainMenuSetCaptainMessage>(OnSetNewCaptain);
        SubscribeAllEvent<CaptainMenuFormFleetMessage>(OnFormFleet);
        SubscribeAllEvent<CaptainMenuRespawnTeamMessage>(OnRespawnTeam);
        SubscribeAllEvent<CaptainMenuDisbandTeamMessage>(OnDisbandTeam);
    }

    /// <summary>
    /// Returns a team currently managed by this session (captain/admiral).
    /// </summary>
    private ShipEventTeam? GetManagedTeam(ICommonSession session)
    {
        if (TryComp<ShipEventTeamMarkerComponent>(session.AttachedEntity, out var marker) && marker.Team != null)
        {
            if (marker.Team.Fleet?.Admiral == session.Channel.UserName)
                return marker.Team.Fleet.ManagedByAdmiral;

            if (marker.Team.Captain == session.Channel.UserName)
                return marker.Team;
        }

        return null;
    }

    private void OnSetNewPassword(CaptainMenuSetPasswordMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team != null)
            team.JoinPassword = msg.Password;
    }

    private void OnSetNewMaxMembers(CaptainMenuSetMaxMembersMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team != null)
            team.MaxMembers = msg.MaxMembers;
    }

    private void OnShipChangeRequest(CaptainMenuChangeShipMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team != null)
        {
            team.ChosenShipType = msg.NewShip;
            TeamMessage(team, Loc.GetString("shipevent-team-ship-changed", ("name", Loc.GetString(team.ChosenShipType.Name))));
        }
    }

    private void OnKickMemberRequest(CaptainMenuKickMemberMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team != null && team.Members.Contains(msg.CKey))
        {
            team.Members.Remove(msg.CKey);
            _chatSys.SendSimpleMessage(Loc.GetString("shipevent-kicked"), session, ChatChannel.Local, Color.DarkRed);
            QueueDel(session.AttachedEntity);
        }
    }

    private void OnSetNewCaptain(CaptainMenuSetCaptainMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team == null)
            return;

        if (msg.CKey == null ||
            !team.Members.Contains(msg.CKey) ||
            !_playerMan.TryGetSessionByUsername(msg.CKey, out var newcap))
            return;

        AssignCaptain(team, newcap);
    }

    private void OnFormFleet(CaptainMenuFormFleetMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team != null)
        {
            if (!TryCreateFleet(team.Name, team.Color, session, out var fleet) ||
                !TryAddTeamToFleet(team, fleet))
            {
                _chatSys.SendSimpleMessage(Loc.GetString("shipevent-capmenu-fleetfailed"), session, color: Color.DarkRed);
                if (fleet != null)
                    RemoveFleet(fleet);
            }
        }
    }

    private void OnRespawnTeam(CaptainMenuRespawnTeamMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team != null)
            QueueTeamRespawn(team);
    }

    private void OnDisbandTeam(CaptainMenuDisbandTeamMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team != null)
            RemoveTeam(team);
    }
}
