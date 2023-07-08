using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarPingsModule : RadarModule
{
    public RadarPingsModule(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void OnKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.Use)
            return;

        args.Handle();
    }
}
