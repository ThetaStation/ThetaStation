using Content.Shared.Theta.Misc;
using Robust.Client.GameObjects;
using Robust.Shared.Audio;

namespace Content.Client.Theta.Misc;

public sealed class SmoothVolumeChangeSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audioSys = default!;

    private Dictionary<uint, (float, float, float)> streams = new(); //stream -> current volume, step, max volume

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SmoothVolumeChangeMessage>(OnVolumeChangeRequest);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        
        foreach (uint streamId in streams.Keys)
        {
            (float curVolume, float step, float maxVolume) = streams[streamId];
            AudioParams parameters = AudioParams.AllNull;
            curVolume += step * frameTime;
            parameters.Volume = curVolume;
            _audioSys.SetAudioParams(streamId, parameters);

            if (curVolume >= maxVolume)
                streams.Remove(streamId);
        }
    }

    private void OnVolumeChangeRequest(SmoothVolumeChangeMessage msg)
    {
        if (msg.TerminateStream)
        {
            if (streams.ContainsKey(msg.StreamId))
                streams.Remove(msg.StreamId);

            return;
        }
        
        streams[msg.StreamId] = (msg.InitialVolume, msg.VolumeChangeSpeed, msg.FinalVolume);
    }
}
