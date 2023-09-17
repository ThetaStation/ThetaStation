using Content.Client.Theta.ModularRadar.Modules;
using Content.Client.Theta.ModularRadar.Modules.ShipEvent;

namespace Content.Client.Theta.ModularRadar.RadarSetups;

public sealed class ModularRadarHUD : ModularRadarControl
{
    public ModularRadarHUD() : base(192f, 192f, 192f)
    {
        Modules.Add(new RadarPosition(this));
        Modules.Add(new RadarGrids(this));
        Modules.Add(new RadarPlayAreaBounds(this));
        Modules.Add(new RadarPingsModule(this));
        Modules.Add(new RadarCommon(this));
    }

    public override int GetUIDisplayRadius()
    {
        return 200;
    }

    public override int GetMinimapMargin()
    {
        return 0;
    }

    protected override Color GetBackground()
    {
        return Color.White.WithAlpha(1);
    }

    protected override Angle GetMatrixRotation()
    {
        return -GetConsoleRotation()!.Value;
    }
}
