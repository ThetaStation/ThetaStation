using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarMobs : RadarModule
{
    private List<MobInterfaceState> _mobs = new();

    public RadarMobs(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not RadarConsoleBoundInterfaceState radarState)
            return;
        _mobs = radarState.MobsAround;
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        foreach (var state in _mobs)
        {
            var position = state.Coordinates.ToMapPos(EntManager);
            var uiPosition = parameters.DrawMatrix.Transform(position);
            var color = Color.Red;

            uiPosition.Y = -uiPosition.Y;

            handle.DrawCircle(ScalePosition(uiPosition), 3f, color);
        }
    }
}
