using System.Numerics;
using Content.Client.Theta.RadarRenderable;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.RadarRenderable;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarCommon : RadarModule
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem;
    [Dependency] private readonly RadarRenderableSystem _radarRenderableSystem;

    private List<CommonRadarEntityInterfaceState> _all = new();
    private RadarRenderableGroup _subscriptions;

    public RadarCommon(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();
        _radarRenderableSystem = EntManager.System<RadarRenderableSystem>();

        _radarRenderableSystem.SubscribeUI(_subscriptions, AddNewRenderableStates);
        _radarRenderableSystem.ClearOldStates += ClearOldStates;
        // подписываться на систему
    }

    private void AddNewRenderableStates(List<CommonRadarEntityInterfaceState> info)
    {
        _all.AddRange(info);
    }

    private void ClearOldStates()
    {
        _all.Clear();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not RadarConsoleBoundInterfaceState radarState)
            return;
        _all = radarState.All;
    }

    public override void OnDispose()
    {
        // убирать подписку от системы
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

            switch (view.Form)
            {
                case RadarEntityViewPrototype.Forms.Circle:
                    var uiPosition = parameters.DrawMatrix.Transform(position);
                    uiPosition.Y = -uiPosition.Y;
                    uiPosition = ScalePosition(uiPosition);
                    handle.DrawCircle(uiPosition, view.Size, color);
                    break;

                case RadarEntityViewPrototype.Forms.FootingTriangle:
                    var footingTriangleVectors = GetVectorsByForm(view, matrix, position, angle);
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, footingTriangleVectors, color);
                    break;

                case RadarEntityViewPrototype.Forms.CenteredTriangle:
                    var centeredTriangleVectors = GetVectorsByForm(view, matrix, position, angle);
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, centeredTriangleVectors, color);
                    break;
                case RadarEntityViewPrototype.Forms.Line:
                    break;
            }
        }
    }

    private Vector2[] GetVectorsByForm(RadarEntityViewPrototype view, Matrix3 matrix, Vector2 position, Angle angle)
    {
        var verts = view.Form switch
        {
            RadarEntityViewPrototype.Forms.FootingTriangle => GetFootingTriangleVectors(view.Size),
            RadarEntityViewPrototype.Forms.CenteredTriangle => GetCenteredTriangleVectors(view.Size),
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
}
