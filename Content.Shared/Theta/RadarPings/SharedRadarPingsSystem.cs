using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.RadarPings;

public abstract class SharedRadarPingsSystem : EntitySystem
{
    protected abstract PingInformation GetPing(EntityUid sender, Vector2 coordinates);

}

[Serializable, NetSerializable]
public sealed class SpreadPingEvent : EntityEventArgs
{
    public EntityUid Sender;
    public Vector2 Coordinates;

    public SpreadPingEvent(EntityUid sender, Vector2 coordinates)
    {
        Sender = sender;
        Coordinates = coordinates;
    }
}

[Serializable, NetSerializable]
public sealed class SendPingEvent: EntityEventArgs
{
    public PingInformation Ping;

    public SendPingEvent(PingInformation ping)
    {
        Ping = ping;
    }
}

[Serializable, NetSerializable]
public record class PingInformation(Vector2 Coordinates, Color Color)
{
    public Vector2 Coordinates = Coordinates;
    public Color Color = Color;
}
