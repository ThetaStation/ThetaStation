using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class CircularShieldOverlay : Overlay
{
    private IEntityManager _entMan = default!;
    private TransformSystem _formSys = default!;

    private const string ShieldFixtureId = "ShieldFixture";

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CircularShieldOverlay(IEntityManager entMan)
    {
        _entMan = entMan;
        _formSys = _entMan.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach ((var form, var shield, var fix) in
                 _entMan.EntityQuery<TransformComponent, CircularShieldComponent, FixturesComponent>())
        {
            if (!shield.CanWork || form.MapID != args.MapId)
                continue;

            PolygonShape? shape = (PolygonShape?) fix.Fixtures.GetValueOrDefault(ShieldFixtureId)?.Shape ?? null;
            if (shape == null)
                continue;

            Vector2[] verts = new Vector2[shape.VertexCount + 1];
            for (var i = 0; i < shape.VertexCount; i++)
            {
                verts[i] = _formSys.GetWorldMatrix(form).Transform(shape.Vertices[i]);
            }

            verts[shape.VertexCount] = verts[0];

            //todo: add fancy shader here
            args.DrawingHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, shield.Color.WithAlpha(0.01f));
            args.DrawingHandle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, shield.Color.WithAlpha(0.1f));
        }
    }
}
