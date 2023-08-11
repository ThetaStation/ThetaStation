using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.RadarRenderable;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarCommon : RadarModule
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private readonly SharedTransformSystem _transformSystem;

    private List<CommonRadarEntityInterfaceState> _all = new();

    public RadarCommon(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not RadarConsoleBoundInterfaceState radarState)
            return;
        _all = radarState.CommonEntities;
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        var matrix = parameters.DrawMatrix;
        foreach (var state in _all)
        {
            var view = _prototypeManager.Index<RadarEntityViewPrototype>(state.RadarViewPrototype);

            var position = state.Coordinates.ToMapPos(EntManager, _transformSystem);
            var angle = state.Angle;

            var color = state.OverrideColor ?? view.DefaultColor;

            switch (view.OnRadarForm)
            {
                case OnRadarForms.Circle:
                    var uiPosition = parameters.DrawMatrix.Transform(position);
                    uiPosition.Y = -uiPosition.Y;
                    uiPosition = ScalePosition(uiPosition);
                    handle.DrawCircle(uiPosition, view.Size, color);
                    break;

                case OnRadarForms.FootingTriangle:
                    var footingTriangleVectors = GetVectorsByForm(view, matrix, position, angle);
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, footingTriangleVectors, color);
                    break;

                case OnRadarForms.CenteredTriangle:
                    var centeredTriangleVectors = GetVectorsByForm(view, matrix, position, angle);
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, centeredTriangleVectors, color);
                    break;
                case OnRadarForms.Line:
                    var lineVectors = GetVectorsByForm(view, matrix, position, angle);
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, lineVectors, color);
                    break;
            }
        }
    }

    private Vector2[] GetVectorsByForm(RadarEntityViewPrototype view, Matrix3 matrix, Vector2 position, Angle angle)
    {
        var verts = view.OnRadarForm switch
        {
            OnRadarForms.FootingTriangle => GetFootingTriangleVectors(view.Size),
            OnRadarForms.CenteredTriangle => GetCenteredTriangleVectors(view.Size),
            OnRadarForms.Line => GetLineVectors(view.Size),
            _ => throw new ArgumentOutOfRangeException()
        };
        for (var i = 0; i < verts.Length; i++)
        {
            verts[i] = matrix.Transform(position + angle.RotateVec(verts[i]));
            verts[i].Y = -verts[i].Y;
            verts[i] = ScalePosition(verts[i]);
        }
        return verts;
    }

    private Vector2[] GetFootingTriangleVectors(float size)
    {
        return new[]
        {
            new Vector2(-size / 2, 0),
            new Vector2(size  / 2, 0),
            new Vector2(0, -size ),
            new Vector2(-size  / 2, 0),
        };
    }

    private Vector2[] GetCenteredTriangleVectors(float size)
    {
        return new[]
        {
            new Vector2(-size / 2, size / 4),
            new Vector2(0, -size / 2 - size / 4),
            new Vector2(size / 2, size / 4),
            new Vector2(-size / 2, size / 4),
        };
    }

    private Vector2[] GetLineVectors(float size)
    {
        return new[]
        {
            new Vector2(0, 0),
            new Vector2(0, size),
        };
    }
}
