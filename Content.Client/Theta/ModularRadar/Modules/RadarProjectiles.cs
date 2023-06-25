using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarProjectiles : RadarModule
{
    private List<ProjectilesInterfaceState> _projectiles = new();

    public RadarProjectiles(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not RadarConsoleBoundInterfaceState radarState)
            return;

        _projectiles = radarState.Projectiles;
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        const float projectileSize = 1.5f;
        var matrix = parameters.DrawMatrix;
        foreach (var state in _projectiles)
        {
            var position = state.Coordinates.ToMapPos(EntManager);
            var angle = state.Angle;
            var color = Color.Brown;

            var verts = new[]
            {
                matrix.Transform(position + angle.RotateVec(new Vector2(-projectileSize / 2, 0))),
                matrix.Transform(position + angle.RotateVec(new Vector2(projectileSize / 2, 0))),
                matrix.Transform(position + angle.RotateVec(new Vector2(0, -projectileSize))),
                matrix.Transform(position + angle.RotateVec(new Vector2(-projectileSize / 2, 0))),
            };
            for (var i = 0; i < verts.Length; i++)
            {
                var vert = verts[i];
                vert.Y = -vert.Y;
                verts[i] = ScalePosition(vert);
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }
}
