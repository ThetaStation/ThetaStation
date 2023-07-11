using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarCannons : RadarModule
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private List<CannonInformationInterfaceState> _cannons = new();

    public RadarCannons(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not RadarConsoleBoundInterfaceState radarState)
            return;

        _cannons = radarState.Cannons;
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        const float cannonSize = 3f;
        foreach (var cannon in _cannons)
        {
            var position = cannon.Coordinates.ToMapPos(EntManager);
            var angle = cannon.Angle;
            var color = cannon.Color;

            var hsvColor = Color.ToHsv(color);

            const float additionalDegreeCoeff = 20f / 360f;

            // X is hue
            var hueOffset = hsvColor.X * cannon.UsedCapacity / Math.Max(1, cannon.MaxCapacity);
            hsvColor.X = Math.Max(hueOffset + additionalDegreeCoeff, additionalDegreeCoeff);

            color = Color.FromHsv(hsvColor);

            var matrix = parameters.DrawMatrix;
            var verts = new[]
            {
                matrix.Transform(position + angle.RotateVec(new Vector2(-cannonSize / 2, cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(0, -cannonSize / 2 - cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(cannonSize / 2, cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(-cannonSize / 2, cannonSize / 4))),
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
