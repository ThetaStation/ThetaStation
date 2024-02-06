using System.Numerics;
using Content.Client.Theta.ShipEvent.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

public sealed class WormholeOverlay : Overlay, IGridOverlay
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    private readonly TransformSystem _formSys = default!;
    private ShaderInstance _shader;

    [Access(typeof(WormholeOverlaySystem))]
    public Dictionary<EntityUid, WormholeOverlayGridParameters> Grids = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceGrids;
    public override bool RequestScreenTexture => true;

    private Entity<MapGridComponent> _grid;
    //those get/set methods are meaningles, yet interface requires them
    public Entity<MapGridComponent> Grid { get => _grid; set { _grid = value; } }
    public bool RequiresFlush { get => true; set { } }

    private const float AttractionStrength = 200;

    public WormholeOverlay()
    {
        IoCManager.InjectDependencies(this);
        _formSys = _entMan.System<TransformSystem>();
        _shader = _protMan.Index<ShaderPrototype>("WormholeOverlay").InstanceUnique();
    }

    private float CalcDuration(Vector2 textureSize, Vector2 attractionCenter, float attractionStrength)
    {
        bool leftClose = attractionCenter.X < textureSize.X / 2;
        bool bottomClose = attractionCenter.Y < textureSize.Y / 2;
        Vector2 farthestPoint = new Vector2(leftClose ? textureSize.X : 0, bottomClose ? textureSize.Y : 0);
        return (farthestPoint - attractionCenter).Length() / attractionStrength;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        foreach (EntityUid uid in Grids.Keys)
        {
            Box2 drawingRect;
            Matrix3 worldMatrix = _formSys.GetWorldMatrix(uid);
            MapGridComponent grid = _entMan.GetComponent<MapGridComponent>(uid);
            drawingRect = worldMatrix.TransformBox(grid.LocalAABB);

            var gridParams = Grids[uid];
            if (gridParams.StartupTime == TimeSpan.Zero)
                gridParams.StartupTime = _timing.CurTime;
            if (gridParams.Duration == 0)
                gridParams.Duration = CalcDuration(drawingRect.Size, gridParams.AttractionCenter, AttractionStrength);
            Vector2 attractionCenterScreen = args.WorldHandle.GetTransform().Transform(gridParams.AttractionCenter);

            Grid = (uid, grid);
            _shader.SetParameter("ScreenTexture", ScreenTexture);
            _shader.SetParameter("ScreenSize", ScreenTexture.Size);
            _shader.SetParameter("AttractionCenter", attractionCenterScreen);
            _shader.SetParameter("AttractionStrength", AttractionStrength);
            _shader.SetParameter("Reverse", gridParams.Reverse ? 1.0f : 0.0f);
            _shader.SetParameter("Time", (float) (_timing.CurTime - gridParams.StartupTime).TotalSeconds);
            _shader.SetParameter("Duration", gridParams.Duration);

            args.DrawingHandle.UseShader(_shader);
            args.WorldHandle.DrawRect(drawingRect, Color.White);
            args.DrawingHandle.UseShader(null);
        }
    }
}
