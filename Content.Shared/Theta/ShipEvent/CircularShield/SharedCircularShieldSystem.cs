using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;

namespace Content.Shared.Theta.ShipEvent.CircularShield;

public class SharedCircularShieldSystem : EntitySystem
{
    public Vector2[] GenerateConeVertices(int radius, Angle angle, Angle width, int extraArcPoints = 0)
    {
        //central point + two edge points + central point again since this is used for drawing and input must be looped = 4
        var vertices = new Vector2[4 + extraArcPoints];
        vertices[0] = new Vector2(0, 0);

        Angle start = angle - width / 2;
        Angle step = width / (2 + extraArcPoints);

        for (var i = 1; i < 3 + extraArcPoints; i++)
        {
            vertices[i] = (start + step * (i - 1)).ToVec() * radius;
        }

        vertices[vertices.Length - 1] = vertices[0];
        return vertices;
    }

    public bool EntityInShield(EntityUid uid, CircularShieldComponent shield, EntityUid otherUid, SharedTransformSystem? formSys = null)
    {
        formSys ??= IoCManager.Resolve<SharedTransformSystem>();
        Vector2 delta = formSys.GetWorldPosition(otherUid) - formSys.GetWorldPosition(uid);
        Angle angle = ThetaHelpers.AngNormal(new Angle(delta) - formSys.GetWorldRotation(uid));
        Angle start = ThetaHelpers.AngNormal(shield.Angle - shield.Width / 2);
        return ThetaHelpers.AngInSector(angle, start, shield.Width) &&
            delta.Length() < shield.Radius + 0.1; //+0.1 to avoid being screwed over by rounding errors
    }
}
