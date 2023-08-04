using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarShieldStatus : RadarModule
{
    [Dependency] private readonly IEntityManager entMan = default!; 
    private readonly TransformSystem formSys = default!;
    
    private const string ShieldFixtureId = "ShieldFixture";
    
    public RadarShieldStatus(ModularRadarControl parentRadar) : base(parentRadar)
    {
        formSys = entMan.System<TransformSystem>();
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        base.Draw(handle, parameters);
        foreach ((var form, var shield, var fix) in entMan.EntityQuery<TransformComponent, CircularShieldComponent, FixturesComponent>())
        {
            PolygonShape? shape = (PolygonShape?)fix.Fixtures.GetValueOrDefault(ShieldFixtureId)?.Shape ?? null;
            if (shape == null)
                continue;
            
            Vector2 pos = formSys.GetWorldPosition(form);
            pos = parameters.DrawMatrix.Transform(pos);
            pos.Y *= -1;
            pos = ScalePosition(pos);
            
            handle.DrawCircle(pos, 5f, shield.Color);
            
            Vector2[] verts = new Vector2[shape.VertexCount + 1];
            for (var i = 0; i < shape.VertexCount; i++)
            {
                Vector2 vert = formSys.GetWorldMatrix(form).Transform(shape.Vertices[i]);
                vert = parameters.DrawMatrix.Transform(vert);
                vert.Y = -vert.Y;
                vert = ScalePosition(vert);
                verts[i] = vert;
            }
            verts[shape.VertexCount] = verts[0];
            
            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, shield.CanWork ? Color.LawnGreen : Color.Red);
        }
    }
}
