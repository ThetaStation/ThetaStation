using Content.Shared.Chat;
using Content.Shared.Roles.Theta;
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
        SubscribeAllEvent<CaptainMenuDisbandTeamMessage>(OnDisbandTeam);
    }

    /// <summary>
    /// Returns a team currently managed by this session (captain/admiral).
    /// </summary>
    private ShipEventTeam? GetManagedTeam(ICommonSession session)
    {
        foreach (ShipEventFleet fleet in Fleets)
        {
            if (fleet.Admiral == session.Channel.UserName)
                return fleet.ManagedByAdmiral;
        }

        foreach (ShipEventTeam team in Teams)
        {
            if (team.Captain == session.Channel.UserName)
                return team;
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

    private void OnDisbandTeam(CaptainMenuDisbandTeamMessage msg)
    {
        if (!_playerMan.TryGetSessionByEntity(msg.Actor, out var session))
            return;

        ShipEventTeam? team = GetManagedTeam(session);
        if (team != null)
            RemoveTeam(team);
    }
}
