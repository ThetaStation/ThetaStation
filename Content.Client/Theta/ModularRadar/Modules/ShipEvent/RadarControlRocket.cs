using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarControlRocket : RadarModule
{
    public List<Vector2> Waypoints = new();

    private const int MaxWaypoints = 15;
    private Color _lineColor = Color.Red;

    public RadarControlRocket(ModularRadarControl parentRadar) : base(parentRadar) { }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        base.Draw(handle, parameters);

        if (Waypoints.Count < 2)
            return;

        for (int i = 0; i < Waypoints.Count - 1; i++)
        {
            Vector2 wp1r = WorldToRelative(Waypoints[i], parameters.DrawMatrix);
            Vector2 wp2r = WorldToRelative(Waypoints[i + 1], parameters.DrawMatrix);
            handle.DrawCircle(wp1r, 2, _lineColor);
            handle.DrawCircle(wp2r, 2, _lineColor);
            handle.DrawDottedLine(wp1r, wp2r, _lineColor, gapSize: 8);
        }
    }

    public override void OnKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.Use)
            return;

        if (Waypoints.Count > MaxWaypoints)
            return;

        Waypoints.Add(RelativeToWorld(args.RelativePosition, OffsetMatrix));

        args.Handle();
    }
}
