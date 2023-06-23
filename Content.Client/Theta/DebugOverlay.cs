using Content.Shared.Theta.ShipEvent;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Theta;

public sealed class DebugOverlaySystem : EntitySystem
{
    private DebugOverlay overlay = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        overlay = new DebugOverlay();
        IoCManager.Resolve<IOverlayManager>().AddOverlay(overlay);
    }
}

public sealed class DebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager entMan = default!;
    private readonly TransformSystem formSys;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public DebugOverlay()
    {
        IoCManager.InjectDependencies(this);
        formSys = entMan.System<TransformSystem>();
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
            }
        }
    }
}
