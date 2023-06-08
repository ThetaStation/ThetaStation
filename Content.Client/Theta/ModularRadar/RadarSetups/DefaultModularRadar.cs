using Content.Client.Theta.ModularRadar.Modules;

namespace Content.Client.Theta.ModularRadar.RadarSetups;

public sealed class DefaultModularRadar : ModularRadarControl
{
    public DefaultModularRadar()
    {
        Modules.Add(new RadarPosition(this));
        Modules.Add(new RadarGrids(this));
        Modules.Add(new RadarDocks(this));
    }
}
