using System.Linq;
using System.Numerics;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Theta.RadarPings;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Server.Theta.RadarPings;

public sealed class RadarPingsSystem : SharedRadarPingsSystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly ShipEventFactionSystem _shipEventSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private Dictionary<ICommonSession, TimeSpan> _playersPingCd = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<SpreadPingEvent>(ReceivePing);
    }

    private void ReceivePing(SpreadPingEvent ev)
    {
        if (!IsValidPing(ev.Sender))
            return;

        var filter = GetPlayersFilter(ev);
        PlaySignalSound(filter, ev.PingOwner);

        var ping = GetPing(ev.PingOwner, ev.Coordinates);
        RaiseNetworkEvent(new SendPingEvent(ping), filter);
    }

    private bool IsValidPing(EntityUid sender)
    {
        if (!_playerManager.TryGetSessionByEntity(sender, out var session))
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

        // Fallback for non-shipevent rounds
        if (_shipEventSystem.RuleSelected)
        {
            var team = _shipEventSystem.TryGetTeamByMember(ev.Sender);
            if (team != null)
            {
                var list = team.Members.Where(r => r.Mind.Session != null).Select(r => team.GetMemberSession(r)!);
                filter.AddPlayers(list);
            }
        }
        else
        {
            filter = Filter.BroadcastGrid(ev.Sender);
        }

        if (_playerManager.TryGetSessionByEntity(ev.Sender, out var session))
            filter.RemovePlayer(session);

        return filter;
    }
}
