using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Content.Shared.Theta.ShipEvent.CircularShield;
using Robust.Shared.Prototypes;
using Vector3 = Robust.Shared.Maths.Vector3;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class CircularShieldOverlay : Overlay
{
    private IEntityManager _entMan = default!;
    private TransformSystem _formSys = default!;
    private SharedCircularShieldSystem _shieldSys = default!;
    private IEyeManager _eyeMan = default!;
    private ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public CircularShieldOverlay()
    {
        _entMan = IoCManager.Resolve<IEntityManager>();
        _formSys = _entMan.System<TransformSystem>();
        _shieldSys = _entMan.System<SharedCircularShieldSystem>();
        _eyeMan = IoCManager.Resolve<IEyeManager>();
        _shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("ShieldOverlay").InstanceUnique();

        _shader.SetParameter("SPEED", 10.0f);
        _shader.SetParameter("BRIGHTNESS", 0.5f);
        _shader.SetParameter("FREQUENCY", 0.5f);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query = _entMan.EntityQuery<CircularShieldComponent, TransformComponent>();
        foreach ((var shield, var form) in query)
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
                verts[i] = Vector2.Transform(verts[i], _formSys.GetWorldMatrix(form));
            }

            Vector2 shieldPos = args.Viewport.WorldToLocal(_formSys.GetWorldPosition(form));
            shieldPos.Y = args.ViewportBounds.Size.Y - shieldPos.Y;

            _shader.SetParameter("BASE_COLOR", new Vector3(shield.Color.R, shield.Color.G, shield.Color.B));
            _shader.SetParameter("CENTER", shieldPos);
            args.WorldHandle.UseShader(_shader);
            args.WorldHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, Color.White);
            args.WorldHandle.UseShader(null);
        }
    }
}
