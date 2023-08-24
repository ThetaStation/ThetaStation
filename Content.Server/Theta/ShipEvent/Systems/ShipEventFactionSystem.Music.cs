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
            Log.Info($"UpdateMusic: Team {team.Name} has {team.DespairLevel} despair points");

            if (team.ActiveMusicConfiguration != null)
            {
                if (team.DespairLevel == 0)
                {
                    Log.Info("Disposing configuration");
                    team.ActiveMusicConfiguration.Dispose();
                    team.ActiveMusicConfiguration = null;
                }
                else
                {
                    Log.Info("Changing intensity");
                    team.ActiveMusicConfiguration.SetIntensity(team.DespairLevel);
                }
            }
            else
            {
                if (team.DespairLevel > 0)
                {
                    Log.Info("Binding new configuration");
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
