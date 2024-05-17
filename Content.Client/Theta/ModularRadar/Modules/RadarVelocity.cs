using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarVelocity : RadarModule
{
    public RadarVelocity(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        var fixturesQuery = EntManager.GetEntityQuery<FixturesComponent>();
        var xformQuery = EntManager.GetEntityQuery<TransformComponent>();
        var bodyQuery = EntManager.GetEntityQuery<PhysicsComponent>();

        var ourGridId = ParentCoordinates!.Value.GetGridUid(EntManager);
        if(ourGridId == null)
            return;

        if (EntManager.HasComponent<MapGridComponent>(ourGridId) &&
            fixturesQuery.HasComponent(ourGridId.Value))
        {
            var transformGridComp = xformQuery.GetComponent(ourGridId.Value);
            var ourGridMatrix = transformGridComp.WorldMatrix;

            Matrix3.Multiply(in ourGridMatrix, in parameters.DrawMatrix, out var matrix);

            var worldRot = transformGridComp.WorldRotation;
            // Get the positive reduced angle.
            var displayRot = -worldRot.Reduced();

            var gridPhysics = bodyQuery.GetComponent(ourGridId.Value);
            var gridVelocity = displayRot.RotateVec(gridPhysics.LinearVelocity);

            DrawVelocityArrow(handle, matrix, gridVelocity, gridPhysics.LocalCenter);
        }
    }

    private void DrawVelocityArrow(DrawingHandleScreen handle, Matrix3 matrix, Vector2 gridVelocity, Vector2 gridCenter)
    {
        const float arrowSize = 3f;

        var (x, y) = (gridVelocity.X, gridVelocity.Y);
        if (x == 0f && y == 0f)
            return;

        var angle = Angle.FromWorldVec(gridVelocity);
        var verts = new[]
        {
            new Vector2(-arrowSize / 2, arrowSize / 4),
            new Vector2(0, -arrowSize / 2 - arrowSize / 4),
            new Vector2(arrowSize / 2, arrowSize / 4),
            new Vector2(arrowSize / 4, arrowSize / 4),
            new Vector2(arrowSize / 4, arrowSize),
            new Vector2(-arrowSize / 4, arrowSize),
            new Vector2(-arrowSize / 4, arrowSize / 4),
            new Vector2(-arrowSize / 2, arrowSize / 4),
        };
        for (var i = 0; i < verts.Length; i++)
        {
            var offset = gridCenter + gridVelocity * 1.5f + angle.RotateVec(verts[i]);
            verts[i] = matrix.Transform(offset);

            var vert = verts[i];
            vert.Y = -vert.Y;
            verts[i] = ScalePosition(vert);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, Color.White);
    }
}
