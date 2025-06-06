﻿using System.Numerics;
using Content.Client.Theta.ShipEvent.Systems;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarPlayAreaBounds : RadarModule
{
    private BoundsOverlaySystem _boundsOverSys;

    public RadarPlayAreaBounds(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _boundsOverSys = EntManager.System<BoundsOverlaySystem>();
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        var matrix = parameters.DrawMatrix;

        Vector2 lb = Vector2.Transform(_boundsOverSys.CurrentBounds.BottomLeft, matrix);
        Vector2 lt = Vector2.Transform(_boundsOverSys.CurrentBounds.TopLeft, matrix);
        Vector2 rb = Vector2.Transform(_boundsOverSys.CurrentBounds.BottomRight, matrix);
        Vector2 rt = Vector2.Transform(_boundsOverSys.CurrentBounds.TopRight, matrix);

        lb.Y = -lb.Y;
        lt.Y = -lt.Y;
        rb.Y = -rb.Y;
        rt.Y = -rt.Y;

        lb = ScalePosition(lb);
        lt = ScalePosition(lt);
        rb = ScalePosition(rb);
        rt = ScalePosition(rt);

        handle.DrawLine(lb, lt, Color.Red);
        handle.DrawLine(rb, rt, Color.Red);
        handle.DrawLine(lb, rb, Color.Red);
        handle.DrawLine(lt, rt, Color.Red);
    }
}
