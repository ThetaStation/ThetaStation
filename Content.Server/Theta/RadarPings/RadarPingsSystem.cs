using System.Numerics;
using Content.Server.Mind;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Theta.RadarPings;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Theta.RadarPings;

public sealed class RadarPingsSystem : SharedRadarPingsSystem
{
    [Dependency] private readonly ShipEventTeamSystem _shipSys = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;

    private Dictionary<ICommonSession, TimeSpan> _playersPingCd = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<SpreadPingEvent>(ReceivePing);
    }

    private void ReceivePing(SpreadPingEvent ev)
    {
        if (!IsValidPing(GetEntity(ev.Sender)))
            return;

        var filter = GetPlayersFilter(ev);
        var evPingOwner = GetEntity(ev.PingOwner);
        PlaySignalSound(filter, evPingOwner);

        var ping = GetPing(evPingOwner, ev.Coordinates);
        RaiseNetworkEvent(new SendPingEvent(ping), filter);
    }

    private bool IsValidPing(EntityUid sender)
    {
        if (!_mindSystem.TryGetMind(sender, out _, out var mind))
            return false;
        if (!_mindSystem.TryGetSession(mind, out var session))
            return false;

        if (_playersPingCd.TryGetValue(session, out var nextPing) && _gameTiming.CurTime < nextPing)
            return false;

        _playersPingCd[session] = _gameTiming.CurTime + NetworkPingCd;
        return true;
    }

    protected override PingInformation GetPing(EntityUid sender, Vector2 coordinates)
    {
        var color = DefaultPingColor;
        if (HasComp<ShuttleConsoleComponent>(sender))
        {
            color = CaptainPingColor;
        }
        else if (HasComp<MobStateComponent>(sender))
        {
            color = MobPingColor;
        }

        return new PingInformation(coordinates, color);
    }

    private Filter GetPlayersFilter(SpreadPingEvent ev)
    {
        var filter = Filter.Empty();
        var senderUid = GetEntity(ev.Sender);

        if (_shipSys.RuleSelected)
        {
            var team = Comp<ShipEventTeamMarkerComponent>(senderUid).Team;
            if (team != null)
                filter.AddPlayers(_shipSys.GetTeamSessions(team));
        }
        else
        {
            // Fallback for non-shipevent rounds
            var gridUid = Transform(senderUid).GridUid;
            if (gridUid != null)
                filter = Filter.BroadcastGrid(gridUid.Value);
        }

        return filter;
    }
}
