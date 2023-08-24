using Content.Shared.Movement.Components;
using Content.Shared.Theta.Misc;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;


public sealed partial class ShipEventFactionSystem
{
    public List<ShipEventMusicConfigurationPrototype> MusicConfigurationPrototypes = new();

    private void UpdateMusic()
    {
        foreach (ShipEventFaction team in Teams)
        {
            if (team.ActiveMusicConfiguration != null)
            {
                if (team.DespairLevel == 0)
                {
                    team.ActiveMusicConfiguration.Dispose();
                    team.ActiveMusicConfiguration = null;
                }
                else
                {
                    team.ActiveMusicConfiguration.SetIntensity(team.DespairLevel);
                }
            }
            else
            {
                if (team.DespairLevel > 0)
                {
                    ShipEventMusicConfiguration configuration = _random.Pick(MusicConfigurationPrototypes).Configuration.ShallowCopy();
                    configuration.Bind(team, _audioSys);
                    team.ActiveMusicConfiguration = configuration;
                }
            }
        }
    }
}


[Prototype("shipEventMusicConfig")]
public sealed class ShipEventMusicConfigurationPrototype : IPrototype
{
    [IdDataField] 
    public string ID { get; } = default!;
    
    [DataField("config", required: true)] 
    public ShipEventMusicConfiguration Configuration = default!;
}

[ImplicitDataDefinitionForInheritors]
public abstract class ShipEventMusicConfiguration
{
    protected ShipEventFaction Team = default!;
    protected AudioSystem AudioSystem = default!;
    
    /*
     todo: this solution is nicer, yet for some reason it also causes IoC to explode
    public byte Intensity
    {
        set
        {
            if (value == _lastIntensity)
                return;
            _lastIntensity = value;

            OnIntensityChange(value);
        }
    }
    */

    public void Bind(ShipEventFaction team, AudioSystem audioSys)
    {
        Team = team;
        AudioSystem = audioSys;
        AfterBind();
    }

    public ShipEventMusicConfiguration ShallowCopy()
    {
        return (ShipEventMusicConfiguration)MemberwiseClone();
    }

    public abstract void AfterBind();

    public abstract void Dispose();

    public abstract void SetIntensity(byte intensity);
}

/// <summary>
/// Single track with linear volume change
/// </summary>
public sealed class ShipEventSimpleMusicConfiguration : ShipEventMusicConfiguration
{
    private IEntityNetworkManager entNetMan = default!;
    
    [DataField("track", required: true)] 
    public SoundSpecifier Track = default!;

    [DataField("maxVolume")]
    public float MaxVolume = 0;
    
    [DataField("minVolume")]
    public float MinVolume = -10;

    [DataField("volumeChangeSpeed")] 
    public float VolumeChangeSpeed = 1;

    private float _currentVolume;

    private IPlayingAudioStream _audioStream = default!;

    public override void AfterBind()
    {
        entNetMan = IoCManager.Resolve<IEntityNetworkManager>();

        if (MaxVolume < MinVolume)
            throw new Exception("Maximum volume is lower than minimum volume.");

        AudioParams parameters = AudioParams.Default;
        parameters.Loop = true;
        parameters.Volume = MinVolume;
        _currentVolume = MinVolume;

        IPlayingAudioStream? stream = AudioSystem.PlayGlobal(Track, Filter.BroadcastGrid(Team.Ship), false, parameters);
        _audioStream = stream ?? throw new Exception("Could not create new audio stream.");
    }

    public override void Dispose()
    {
        entNetMan.SendSystemNetworkMessage(new SmoothVolumeChangeMessage(
            _audioStream, 
            0, 
            0,
            0,
            true));
        
        _audioStream.Stop();
    }

    public override void SetIntensity(byte intensity)
    {
        float newVolume = MinVolume + (float)intensity / byte.MaxValue * (MaxVolume - MinVolume);
        
        entNetMan.SendSystemNetworkMessage(new SmoothVolumeChangeMessage( //same as RaiseNetworkEvent() from ent system
            _audioStream, 
            _currentVolume, 
            newVolume,
            VolumeChangeSpeed,
            false));

        _currentVolume = newVolume;
    }
}

/// <summary>
/// Multiple tracks being layered onto each other as intensity is raising
/// </summary>
public sealed class ShipEventLayeredMusicConfiguration : ShipEventMusicConfiguration
{
    private IEntityNetworkManager entNetMan = default!;
    
    [DataField("tracks", required: true)]
    public Dictionary<byte, SoundSpecifier> Tracks = default!;

    private List<IPlayingAudioStream> _streams = new();

    private Filter _teamFilter = default!;

    [DataField("volume")] 
    public float Volume = 0;
    
    [DataField("minVolume")] 
    public float MinVolume = -10;

    public override void AfterBind()
    {
        entNetMan = IoCManager.Resolve<IEntityNetworkManager>();
        _teamFilter = Filter.BroadcastGrid(Team.Ship);
    }

    public override void Dispose()
    {
        foreach (IPlayingAudioStream stream in _streams)
        {
            entNetMan.SendSystemNetworkMessage(new SmoothVolumeChangeMessage(
                stream, 
                0, 
                0,
                0,
                true));
        
            stream.Stop();
        }
    }

    public override void SetIntensity(byte intensity)
    {
        int count = 0;
        foreach (byte trackIntensity in Tracks.Keys)
        {
            count++;
            if (trackIntensity <= intensity)
            {
                if (_streams.Count < count)
                {
                    AudioParams parameters = AudioParams.Default;
                    parameters.Loop = true;
                    parameters.Volume = MinVolume;
                    
                    IPlayingAudioStream? stream = AudioSystem.PlayGlobal(Tracks[trackIntensity], _teamFilter, false, parameters);
                    if (stream == null)
                        throw new Exception("Could not create new audio stream.");
                    
                    _streams.Add(stream);
                }
            }
            else
            {
                if (_streams.Count >= count)
                    _streams.RemoveRange(count - 1, _streams.Count - count + 1); //remove all tracks with higher min intensity
                return;
            }
        }
    }
}
