using System.Linq;
using System.Numerics;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Theta.RadarPings;
using Robust.Shared.Player;
using Robust.Shared.Players;

namespace Content.Server.Theta.RadarPings;

public sealed class RadarPingsSystem : SharedRadarPingsSystem
{
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly ShipEventFactionSystem _shipEventSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SpreadPingEvent>(ReceivePing);
    }

    private void ReceivePing(SpreadPingEvent ev)
    {
        var filter = GetPlayersFilter(ev);
        PlaySignalSound(filter, ev.PingOwner);

        var ping = GetPing(ev.PingOwner, ev.Coordinates);
        RaiseNetworkEvent(new SendPingEvent(ping), filter);
    }

    protected override PingInformation GetPing(EntityUid sender, Vector2 coordinates)
    {
        var color = Color.Blue;
        if (HasComp<ShuttleConsoleComponent>(sender))
            color = Color.Red;

        return new PingInformation(coordinates, color);
    }

    private Filter GetPlayersFilter(SpreadPingEvent ev)
    {
        var filter = Filter.Empty();

        // Fallback for non-shipevent rounds
        if (_shipEventSystem.RuleSelected)
        {
            foreach (var team in _shipEventSystem.Teams)
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
