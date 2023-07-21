using Content.Client.Theta.ModularRadar.Modules;
using Content.Client.Theta.ModularRadar.Modules.ShipEvent;

namespace Content.Client.Theta.ModularRadar.RadarSetups;

public sealed class CannonModularRadar : ModularRadarControl
{
    public CannonModularRadar()
    {
        Modules.Add(new RadarPosition(this));
        Modules.Add(new RadarGrids(this));
        Modules.Add(new RadarPlayAreaBounds(this));
        Modules.Add(new RadarLootboxTimer(this));
        Modules.Add(new RadarCannons(this));
        Modules.Add(new RadarControlCannons(this));
        Modules.Add(new RadarProjectiles(this));
        Modules.Add(new RadarMobs(this));
        Modules.Add(new RadarVelocity(this));
    }
}
