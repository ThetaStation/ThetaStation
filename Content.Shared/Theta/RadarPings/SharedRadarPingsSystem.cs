using System.Numerics;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.RadarPings;

public abstract class SharedRadarPingsSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    private const string PingSound = "/Audio/Theta/Shipevent/radar_ping.ogg";
    protected readonly Color DefaultPingColor = Color.Blue;
    protected readonly Color CaptainPingColor = Color.Red;
    protected readonly Color MobPingColor = Color.LightGreen;


    protected abstract PingInformation GetPing(EntityUid sender, Vector2 coordinates);

    protected void PlaySignalSound(Filter hearer, EntityUid from)
    {
        _audioSystem.Play(PingSound, hearer, from, false);
    }
}

[Serializable, NetSerializable]
public sealed class SpreadPingEvent : EntityEventArgs
{
    public EntityUid Sender;
    public EntityUid PingOwner;
    public Vector2 Coordinates;

    public SpreadPingEvent(EntityUid sender, EntityUid pingOwner, Vector2 coordinates)
    {
        Sender = sender;
        PingOwner = pingOwner;
        Coordinates = coordinates;
    }
}

[Serializable, NetSerializable]
public sealed class SendPingEvent : EntityEventArgs
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
