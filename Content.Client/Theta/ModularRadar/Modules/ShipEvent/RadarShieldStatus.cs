using System.Numerics;
using Content.Shared.Theta.ShipEvent.CircularShield;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarShieldStatus : RadarModule
{
    private readonly SharedCircularShieldSystem _shieldSys;
    private readonly TransformSystem _formSys;

    public RadarShieldStatus(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _shieldSys = EntManager.System<SharedCircularShieldSystem>();
        _formSys = EntManager.System<TransformSystem>();
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        base.Draw(handle, parameters);

        var ourGridId = ParentCoordinates!.Value.GetGridUid(EntManager);
        var rot = _formSys.GetWorldRotation(ourGridId!.Value);
        var query = EntManager.EntityQueryEnumerator<CircularShieldComponent>();
        while (query.MoveNext(out var uid, out var shield))
        {
            if (!shield.Powered)
                continue;

            var position = _formSys.GetWorldPosition(uid);
            var color = shield.Color;

            var verts = _shieldSys.GenerateConeVertices(shield.Radius, shield.Angle, shield.Width, (int) (shield.Width / Math.Tau * 20));
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = parameters.DrawMatrix.Transform(position + rot.RotateVec(verts[i]));
                verts[i].Y = -verts[i].Y;
                verts[i] = ScalePosition(verts[i]);
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color.WithAlpha(0.1f));
            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }
}
