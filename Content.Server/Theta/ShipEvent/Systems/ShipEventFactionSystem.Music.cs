using Robust.Server.GameObjects;
using Robust.Shared.Audio;
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
            Log.Debug("UpdateMusic: Team {} has {} despair points");

            if (team.ActiveMusicConfiguration != null)
            {
                if (team.DespairLevel == 0)
                {
                    Log.Debug("Disposing configuration");
                    team.ActiveMusicConfiguration.Dispose();
                    team.ActiveMusicConfiguration = null;
                }
                else
                {
                    Log.Debug("Changing intensity");
                    team.ActiveMusicConfiguration.Intensity = team.DespairLevel;
                }
            }
            else
            {
                if (team.DespairLevel > 0)
                {
                    Log.Debug("Binding new configuration");
                    ShipEventMusicConfiguration configuration = _random.Pick(MusicConfigurationPrototypes).Configuration.ShallowCopy();
                    configuration.Bind(team, _audioSys);
                    team.ActiveMusicConfiguration = configuration;
                }
            }

            team.DespairLevel -= 5;
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

    public byte Intensity
    {
        set
        {
            if (_lastIntensity == value)
                return;
            _lastIntensity = value;
            OnIntensityChange(value);
        }
    }
    
    private byte _lastIntensity;

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

    public abstract void OnIntensityChange(byte intensity);
}

/// <summary>
/// Single track with linear volume change
/// </summary>
public sealed class ShipEventSimpleMusicConfiguration : ShipEventMusicConfiguration
{
    [DataField("track", required: true)] 
    public SoundSpecifier? Track;

    [DataField("maxVolume")]
    public int MaxVolume = -1;
    
    [DataField("minVolume")]
    public int MinVolume = -10;

    private IPlayingAudioStream? _audioStream;

    public override void AfterBind()
    {
        if (MaxVolume < MinVolume)
            throw new Exception("Maximum volume is lower than minimum volume.");

        Filter teamFilter = Filter.BroadcastGrid(Team.Ship);

        AudioParams parameters = AudioParams.Default;
        parameters.Loop = true;
        parameters.Volume = 1;
        
        _audioStream = AudioSystem.PlayGlobal(Track, teamFilter, false, parameters);
    }

    public override void Dispose()
    {
        _audioStream?.Stop();
    }

    public override void OnIntensityChange(byte intensity)
    {
        if (_audioStream == null)
        {
            Logger.Error("ShipEventSimpleMusicConfiguration: Audio stream is null but configuration is still in use!");
            return;
        }
        
        AudioParams parameters = AudioParams.Default;
        parameters.Volume = MinVolume + intensity / byte.MaxValue * (MaxVolume - MinVolume);
        
        AudioSystem.SetAudioParams(_audioStream, parameters);
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

    public override void OnIntensityChange(byte intensity)
    {
        
    }
}

