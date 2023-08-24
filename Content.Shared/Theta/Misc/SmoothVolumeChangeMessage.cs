using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.Misc;

[Serializable, NetSerializable]
public sealed class SmoothVolumeChangeMessage : EntityEventArgs
{
    public uint StreamId;
    public float InitialVolume;
    public float FinalVolume;
    public float VolumeChangeSpeed; // dB/s
    public bool TerminateStream;

    public SmoothVolumeChangeMessage(IPlayingAudioStream stream, float initialVolume, float finalVolume, float volumeChangeSpeed, bool terminateStream)
    {
        StreamId = stream.Identifier;
        InitialVolume = initialVolume;
        FinalVolume = finalVolume;
        VolumeChangeSpeed = volumeChangeSpeed;
        TerminateStream = terminateStream;
    }
}
