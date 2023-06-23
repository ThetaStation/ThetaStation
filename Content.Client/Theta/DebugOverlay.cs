using Content.Shared.Theta.ShipEvent;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Theta;

public sealed class DebugOverlayReceiveDirsEvent : EntityEventArgs
{
    public Vector2 Source;
    public List<Vector2> Dirs;

    public DebugOverlayReceiveDirsEvent(Vector2 src, List<Vector2> dirs)
    {
        Source = src;
        Dirs = dirs;
    }
}

public sealed class DebugOverlaySystem : EntitySystem
{
    private DebugOverlay overlay = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        overlay = new DebugOverlay();
        IoCManager.Resolve<IOverlayManager>().AddOverlay(overlay);
        SubscribeLocalEvent<DebugOverlayReceiveDirsEvent>(AddDirs);
    }

    private void AddDirs(DebugOverlayReceiveDirsEvent ev)
    {
        overlay.Dirs.Add((overlay.GetColor(), ev.Source, ev.Dirs));
    }
}

public sealed class DebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager entMan = default!;
    private readonly TransformSystem formSys;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public List<(Color, Vector2, List<Vector2>)> Dirs = new();

    private List<Color> colors;

    private int lastColor = -1;
    
    public DebugOverlay()
    {
        IoCManager.InjectDependencies(this);
        formSys = entMan.System<TransformSystem>();
        colors = new()
        {
            Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Cyan, Color.Blue, Color.DeepPink, Color.Purple
        };
    }

    public Color GetColor()
    {
        lastColor++;
        return colors[lastColor];
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach ((TransformComponent form, CannonComponent cannon) in entMan.EntityQuery<TransformComponent, CannonComponent>())
        {
            foreach ((Angle a, Angle b) in cannon.ObstructedRanges)
            {
                Angle wr = formSys.GetWorldRotation(form.ParentUid);
                Vector2 wp = formSys.GetWorldPosition(form);
                Vector2 s = wp + (a+wr).ToVec() * 2;
                Vector2 e = wp + (b+wr).ToVec() * 2;
                
                args.WorldHandle.DrawCircle(wp, 0.1f, Color.Yellow);
                args.WorldHandle.DrawLine(wp, s, Color.Red);
                args.WorldHandle.DrawLine(wp, e, Color.Blue);
                args.WorldHandle.DrawLine(s, e, Color.Yellow);
            }
        }

        /*
        foreach ((Color c, Vector2 src, List<Vector2> dirs) in Dirs)
        {
            foreach (Vector2 dir in dirs)
            {
                Vector2 r = src + dir;
                args.WorldHandle.DrawLine(src, r,c);
                args.WorldHandle.DrawRect(new Box2(r.X - 0.1f, r.Y - 0.1f, r.X + 0.1f, r.Y + 0.1f), c, false);
            }
        }
        */
    }
}
