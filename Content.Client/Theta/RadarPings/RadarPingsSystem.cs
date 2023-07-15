using System.Numerics;
using Content.Client.Shuttles;
using Content.Shared.Theta.RadarPings;

namespace Content.Client.Theta.RadarPings;

public sealed class RadarPingsSystem : SharedRadarPingsSystem
{
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

    public PingInformation SendPing(EntityUid sender, Vector2 coordinates)
    {
        RaiseNetworkEvent(new SpreadPingEvent(sender, coordinates));

        var ping = GetPing(sender, coordinates);
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
