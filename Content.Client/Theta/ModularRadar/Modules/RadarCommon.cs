using System.Numerics;
using System.Text;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.RadarRenderable;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarCommon : RadarModule
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    private readonly SharedTransformSystem _transformSystem;
    private readonly SpriteSystem _spriteSystem;
    private readonly Font _font;

    private List<CommonRadarEntityInterfaceState> _all = new();

    public RadarCommon(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _transformSystem = EntManager.System<SharedTransformSystem>();
        _spriteSystem = EntManager.System<SpriteSystem>();
        _font = new VectorFont(_resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
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

            var position = EntManager.GetCoordinates(state.Coordinates).ToMapPos(EntManager, _transformSystem);
            var angle = state.Angle;

            var color = state.OverrideColor ?? view.DefaultColor;

            switch (view.OnRadarForm)
            {
                case CircleRadarForm circleRadarForm:
                    var uiPosition = matrix.Transform(position);
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
                    handle.DrawPrimitives(GetTopology((SharedDrawPrimitiveTopology) shapeRadarForm.PrimitiveTopology), verts, color);
                    break;
                case CharRadarForm charRadarForm:
                    var uiPositionChar = matrix.Transform(position);
                    uiPositionChar.Y = -uiPositionChar.Y;
                    uiPositionChar = ScalePosition(uiPositionChar);
                    _font.DrawChar(handle, new Rune(charRadarForm.Char), uiPositionChar, charRadarForm.Scale, color);
                    break;
                case TextureRadarForm textureRadarForm:
                    var uiPositionTexture = matrix.Transform(position);
                    uiPositionTexture.Y = -uiPositionTexture.Y;
                    uiPositionTexture = ScalePosition(uiPositionTexture);

                    var texture = _spriteSystem.Frame0(textureRadarForm.Sprite);
                    var textureSize = texture.Size * textureRadarForm.Scale;
                    var box = UIBox2.FromDimensions(uiPositionTexture - (textureSize * 0.5f), textureSize);

                    handle.DrawTextureRect(texture, box);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private DrawPrimitiveTopology GetTopology(SharedDrawPrimitiveTopology topology)
    {
        return topology switch
        {
            SharedDrawPrimitiveTopology.TriangleList => DrawPrimitiveTopology.TriangleList,
            SharedDrawPrimitiveTopology.TriangleFan => DrawPrimitiveTopology.TriangleFan,
            SharedDrawPrimitiveTopology.TriangleStrip => DrawPrimitiveTopology.TriangleStrip,
            SharedDrawPrimitiveTopology.LineList => DrawPrimitiveTopology.LineList,
            SharedDrawPrimitiveTopology.LineStrip => DrawPrimitiveTopology.LineStrip,
            SharedDrawPrimitiveTopology.LineLoop => DrawPrimitiveTopology.LineLoop,
            SharedDrawPrimitiveTopology.PointList => DrawPrimitiveTopology.PointList,
            _ => DrawPrimitiveTopology.TriangleList
        };
    }
}
