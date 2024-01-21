using System.Numerics;
using Content.Client.Theta.ShipEvent.Systems;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarShieldStatus : RadarModule
{
    private readonly CircularShieldSystem _shieldSys;
    private readonly TransformSystem _formSys;

    public RadarShieldStatus(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _shieldSys = EntManager.System<CircularShieldSystem>();
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

            var position = _formSys.GetWorldPosition(EntManager.GetComponent<TransformComponent>(uid));
            var color = shield.Color;

            var cone = _shieldSys.GenerateConeVertices(shield.Radius, shield.Angle, shield.Width, 5);
            var verts = new Vector2[cone.Length + 1];
            for (var i = 0; i < cone.Length; i++)
            {
                verts[i] = cone[i];
                verts[i] = parameters.DrawMatrix.Transform(position + rot.RotateVec(verts[i]));
                verts[i].Y = -verts[i].Y;
                verts[i] = ScalePosition(verts[i]);
            }

            verts[cone.Length] = verts[0];
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color.WithAlpha(0.1f));
            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }
}
