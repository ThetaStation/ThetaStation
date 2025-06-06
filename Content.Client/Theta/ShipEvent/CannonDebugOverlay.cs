using System.Numerics;
using Content.Shared.Theta.ShipEvent;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Theta;

// uncomment when needed, I can't be bothered to make a command for that
/*
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
        foreach ((var cannon, var form) in entMan.EntityQuery<CannonComponent, TransformComponent>(true))
        {
            if (!form.ParentUid.IsValid())
                continue;
            Angle wr = formSys.GetWorldRotation(form.ParentUid);
            Vector2 wp = formSys.GetWorldPosition(form);

            foreach ((Angle s, Angle w) in cannon.ObstructedRanges)
            {
                Vector2 sv = wp + (s + wr).ToVec() * 5;
                Vector2 ev = wp + (s + w + wr).ToVec() * 5;

                args.WorldHandle.DrawCircle(wp, 0.1f, Color.Yellow);
                args.WorldHandle.DrawLine(wp, sv, Color.Red);
                args.WorldHandle.DrawLine(wp, ev, Color.Blue);
                args.WorldHandle.DrawLine(wp, wp + (s + wr + w / 2).ToVec() * 5, Color.Yellow);
            }
        }
    }
}
*/
