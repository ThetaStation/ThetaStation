using System.Numerics;

namespace Content.Shared.Theta.ShipEvent.CircularShield;

public abstract class SharedCircularShieldSystem : EntitySystem
{
    public Vector2[] GenerateConeVertices(int radius, Angle angle, Angle width, int extraArcPoints = 0)
    {
        var vertices = new Vector2[3 + extraArcPoints];
        vertices[0] = new Vector2(0, 0);

        Angle start = angle - width / 2;
        Angle step = width / (2 + extraArcPoints);

        for (var i = 1; i < 3 + extraArcPoints; i++)
        {
            vertices[i] = (start + step * (i - 1)).ToVec() * radius;
        }

        return vertices;
    }
}
