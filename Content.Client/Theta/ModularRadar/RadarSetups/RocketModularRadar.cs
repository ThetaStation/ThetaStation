using Content.Client.Theta.ModularRadar.Modules;
using Content.Client.Theta.ModularRadar.Modules.ShipEvent;

namespace Content.Client.Theta.ModularRadar.RadarSetups;

public sealed class RocketModularRadar : ModularRadarControl
{
    public RocketModularRadar()
    {
        Modules.Add(new RadarPosition(this));
        Modules.Add(new RadarGrids(this));
        Modules.Add(new RadarPlayAreaBounds(this));
        Modules.Add(new RadarPingsModule(this));
        Modules.Add(new RadarControlRocket(this));
        Modules.Add(new RadarCommon(this));
    }
}
