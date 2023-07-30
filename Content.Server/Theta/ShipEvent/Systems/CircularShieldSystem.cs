using System.Numerics;
using Content.Server.Power.Components;
using Content.Shared.Physics;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Theta.ShipEvent.Systems;


public sealed class CircularShieldSystem : EntitySystem
{
    [Dependency] public readonly PhysicsSystem physSys = default!;
    [Dependency] public readonly FixtureSystem fixSys = default!;

    private const string ShieldFixtureId = "ShieldFixture";
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldChangeParametersMessage>(OnShieldChangeParams);
        SubscribeLocalEvent<CircularShieldComponent, ComponentInit>(OnShieldInit);
        SubscribeLocalEvent<CircularShieldComponent, PowerChangedEvent>((uid, shield, args) => shield.Powered = args.Powered);
        SubscribeLocalEvent<CircularShieldComponent, StartCollideEvent>(OnShieldFixtureEnter);
        SubscribeLocalEvent<CircularShieldComponent, EndCollideEvent>(OnShieldFixtureExit);
    }

    private void OnShieldChangeParams(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldChangeParametersMessage args)
    {
        if(console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        if (args.Radius > shield.MaxRadius || args.Width > shield.MaxWidth)
            return;

        shield.Enabled = args.Enabled;
        shield.Radius = args.Radius;
        shield.Width = args.Width;
        UpdateShieldFixture(uid, shield);

        if (TryComp<ApcPowerReceiverComponent>(uid, out ApcPowerReceiverComponent? receiver))
        {
            receiver.Load = shield.DesiredDraw;
        }
        else
        {
            if(shield.DesiredDraw > 0)
                shield.Powered = false;
        }
    }
    
    private void OnShieldInit(EntityUid uid, CircularShieldComponent shield, ComponentInit args)
    {
        foreach (CircularShieldEffect effect in shield.Effects)
        {
            effect.OnShieldInit(uid, shield);
        }
    }
    
    private void OnShieldFixtureEnter(EntityUid uid, CircularShieldComponent shield, ref StartCollideEvent args)
    {
        if (!shield.CanWork || args.OurFixture.ID != ShieldFixtureId)
            return;
        
        foreach (CircularShieldEffect effect in shield.Effects)
        {
            effect.OnShieldEnter(args.OtherEntity, shield);
        }
    }
    
    private void OnShieldFixtureExit(EntityUid uid, CircularShieldComponent shield, ref EndCollideEvent args)
    {
        if (!shield.CanWork || args.OurFixture.ID != ShieldFixtureId)
            return;

        foreach (CircularShieldEffect effect in shield.Effects)
        {
            effect.OnShieldExit(args.OtherEntity, shield);
        }
    }

    private void UpdateShieldFixture(EntityUid uid, CircularShieldComponent shield, int extraArcPoints = 0)
    {
        Fixture? shieldFix = fixSys.GetFixtureOrNull(uid, ShieldFixtureId);
        if (shieldFix == null)
        {
            int layer = (int)(CollisionGroup.Impassable & CollisionGroup.BulletImpassable);
            fixSys.TryCreateFixture(uid, new PolygonShape(), ShieldFixtureId, hard: false, collisionLayer: layer);
            shieldFix = fixSys.GetFixtureOrNull(uid, ShieldFixtureId);
        }
        
        Vector2[] vertices = new Vector2[3 + extraArcPoints];
        vertices[0] = new Vector2(0, 0);

        Angle start = shield.Angle - shield.Width / 2;
        Angle step = shield.Width / (2 + extraArcPoints);
        
        for (int i = 1; i < 3 + extraArcPoints; i++)
        {
            vertices[i] = (start + step * (i - 1)).ToVec() * shield.Radius;
        }

        physSys.SetVertices(uid, shieldFix!, new PolygonShape(), vertices);
    }
}
