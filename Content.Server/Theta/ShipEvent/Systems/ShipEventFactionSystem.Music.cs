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
    public SoundSpecifier? Track;

    [DataField("maxVolume")]
    public float MaxVolume = -1;
    
    [DataField("minVolume")]
    public float MinVolume = -10;

    [DataField("volumeChangeSpeed")] 
    public float VolumeChangeSpeed = 2;

    private float _currentVolume;

    private IPlayingAudioStream? _audioStream;

    public override void AfterBind()
    {
        entNetMan = IoCManager.Resolve<IEntityNetworkManager>();

        if (MaxVolume < MinVolume)
            throw new Exception("Maximum volume is lower than minimum volume.");

        AudioParams parameters = AudioParams.Default;
        parameters.Loop = true;
        parameters.Volume = MinVolume;
        
        Filter teamFilter = Filter.BroadcastGrid(Team.Ship);
        
        _audioStream = AudioSystem.PlayGlobal(Track, teamFilter, false, parameters);
    }

    public override void Dispose()
    {
        if (_audioStream == null)
            return;

        entNetMan.SendSystemNetworkMessage(new SmoothVolumeChangeMessage( //same as RaiseNetworkEvent() from ent system
            _audioStream, 
            0, 
            0,
            0,
            true));
        
        _audioStream.Stop();
    }

    public override void SetIntensity(byte intensity)
    {
        if (_audioStream == null) 
            return;

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
    public override void AfterBind()
    {
        
    }

    public override void Dispose()
    {
        
    }

    public override void SetIntensity(byte intensity)
    {
        
    }
}
