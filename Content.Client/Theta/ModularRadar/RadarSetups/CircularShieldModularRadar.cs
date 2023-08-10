using Content.Client.Theta.ModularRadar.Modules;
using Content.Client.Theta.ModularRadar.Modules.ShipEvent;

namespace Content.Client.Theta.ModularRadar.RadarSetups;


public sealed class CircularShieldModularRadar : ModularRadarControl
{
    public CircularShieldModularRadar()
    {
        Modules.Add(new RadarPosition(this));
        Modules.Add(new RadarGrids(this));
        Modules.Add(new RadarPlayAreaBounds(this));
        Modules.Add(new RadarPingsModule(this));
        Modules.Add(new RadarShieldStatus(this));
        Modules.Add(new RadarControlShield(this));
    }
}
