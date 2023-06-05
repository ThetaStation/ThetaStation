using Content.Client.Theta.ModularRadar.Modules;

namespace Content.Client.Theta.ModularRadar.RadarSetups;

public sealed class DefaultModularRadar : ModularRadarControl
{
    public DefaultModularRadar()
    {
        Modules.Add(new RadarPosition());
        Modules.Add(new RadarGrids());
        Modules.Add(new RadarDocks());
    }
}
