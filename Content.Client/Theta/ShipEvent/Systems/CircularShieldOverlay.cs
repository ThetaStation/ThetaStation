using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Physics.Systems;
using Content.Shared.Theta.ShipEvent.CircularShield;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class CircularShieldOverlay : Overlay
{
    private IEntityManager _entMan = default!;
    private TransformSystem _formSys = default!;
    private SharedCircularShieldSystem _shieldSys = default!;

    private const string ShieldFixtureId = "ShieldFixture";

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CircularShieldOverlay(IEntityManager entMan)
    {
        _entMan = entMan;
        _formSys = _entMan.System<TransformSystem>();
        _shieldSys = _entMan.System<SharedCircularShieldSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        foreach ((var form, var shield) in _entMan.EntityQuery<TransformComponent, CircularShieldComponent>())
        {
            if (!shield.CanWork || form.MapID != args.MapId)
                continue;

            Vector2[] verts = _shieldSys.GenerateConeVertices(
                shield.Radius,
                shield.Angle,
                shield.Width,
                (int) (shield.Width / Math.Tau * 20));
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = _formSys.GetWorldMatrix(form).Transform(verts[i]);
            }

            //todo: add fancy shader here
            args.DrawingHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, shield.Color.WithAlpha(0.01f));
            args.DrawingHandle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, shield.Color.WithAlpha(0.1f));
        }
    }
}
