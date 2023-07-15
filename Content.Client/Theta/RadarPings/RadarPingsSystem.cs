using System.Numerics;
using Content.Client.Shuttles;
using Content.Shared.Theta.RadarPings;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Theta.RadarPings;

public sealed class RadarPingsSystem : SharedRadarPingsSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    public event Action<PingInformation>? OnEventReceived;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SendPingEvent>(ReceivePing);
    }

    private void ReceivePing(SendPingEvent ev)
    {
        OnEventReceived?.Invoke(ev.Ping);
    }

    public PingInformation SendPing(EntityUid pingOwner, Vector2 coordinates)
    {
        var sender = _playerManager.LocalPlayer!.ControlledEntity!.Value;
        RaiseNetworkEvent(new SpreadPingEvent(sender, pingOwner, coordinates));

        var ping = GetPing(pingOwner, coordinates);
        PlaySignalSound(Filter.Entities(sender), pingOwner);
        return ping;
    }

    protected override PingInformation GetPing(EntityUid sender, Vector2 coordinates)
    {
        var color = Color.Blue;
        if (HasComp<ShuttleConsoleComponent>(sender))
            color = Color.Red;

        return new PingInformation(coordinates, color);
    }
}
