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
                case CircleRadarForm circleRadarForm:
                    var uiPosition = parameters.DrawMatrix.Transform(position);
                    uiPosition.Y = -uiPosition.Y;
                    uiPosition = ScalePosition(uiPosition);
                    handle.DrawCircle(uiPosition, circleRadarForm.Radius, color);
                    break;
                case ShapeRadarForm shapeRadarForm:
                    var verts = new Vector2[shapeRadarForm.Vertices.Length];
                    shapeRadarForm.Vertices.CopyTo(verts, 0);
                    for (var i = 0; i < verts.Length; i++)
                    {
                        verts[i] *= shapeRadarForm.Size;
                        verts[i] = matrix.Transform(position + angle.RotateVec(verts[i]));
                        verts[i].Y = -verts[i].Y;
                        verts[i] = ScalePosition(verts[i]);
                    }
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

}
