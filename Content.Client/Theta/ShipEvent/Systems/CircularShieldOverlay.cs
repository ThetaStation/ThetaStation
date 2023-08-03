using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class CircularShieldOverlay : Overlay
{
    private IEntityManager entMan = default!;
    private TransformSystem formSys = default!;
    private FixtureSystem fixSys = default!;
    
    private const string ShieldFixtureId = "ShieldFixture";
    
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CircularShieldOverlay(IEntityManager _entMan)
    {
        entMan = _entMan;
        formSys = entMan.System<TransformSystem>();
        fixSys = entMan.System<FixtureSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach ((var form, var shield, var fix) in 
                 entMan.EntityQuery<TransformComponent, CircularShieldComponent, FixturesComponent>())
        {
            if (!shield.CanWork || form.MapID != args.MapId)
                continue;

            PolygonShape? shape = (PolygonShape?)fix.Fixtures.GetValueOrDefault(ShieldFixtureId)?.Shape ?? null;
            if (shape == null)
                continue;

            Vector2[] verts = new Vector2[shape.VertexCount];
            for (var i = 0; i < verts.Length; i++)
            {
                verts[i] = formSys.GetWorldMatrix(form).Transform(shape.Vertices[i]);
            }

            //todo: add fancy shader here
            args.DrawingHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, shield.Color.WithAlpha(0.1f));
        }
    }
}
