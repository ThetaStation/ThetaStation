using System.Numerics;
using Content.Server.Power.Components;
using Content.Shared.Physics;
using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
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
    [Dependency] public readonly UserInterfaceSystem uiSys = default!;

    private const string ShieldFixtureId = "ShieldFixture";
    
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldChangeParametersMessage>(OnShieldChangeParams);
        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldConsoleInfoRequest>(OnConsoleInfoRequest);

        SubscribeLocalEvent<CircularShieldComponent, ComponentInit>(OnShieldInit);
        SubscribeLocalEvent<CircularShieldComponent, PowerChangedEvent>(OnShieldPowerChanged);
        SubscribeLocalEvent<CircularShieldComponent, StartCollideEvent>(OnShieldFixtureEnter);
        SubscribeLocalEvent<CircularShieldComponent, EndCollideEvent>(OnShieldFixtureExit);
    }

    private void OnConsoleInfoRequest(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldConsoleInfoRequest args)
    {
        SendShieldConsoleUpdates(uid);
    }

    private void SendShieldConsoleUpdates(EntityUid uid)
    {
        if (!TryComp(uid, out CircularShieldConsoleComponent? console) || console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        uiSys.TrySetUiState(uid, CircularShieldConsoleUiKey.Key, new CircularShieldConsoleWindowBoundsUserInterfaceState(
            shield.Enabled,
            shield.Powered,
            (int)shield.Angle.Degrees,
            (int)shield.Width.Degrees,
            shield.MaxWidth,
            shield.Radius,
            shield.MaxRadius));
    }

    private void OnShieldChangeParams(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldChangeParametersMessage args)
    {
        if(console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        if (args.Radius > shield.MaxRadius || args.Width.Degrees > shield.MaxWidth)
            return;

        shield.Enabled = args.Enabled;
        shield.Angle = args.Angle;
        shield.Width = args.Width;
        shield.Radius = args.Radius;
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

    private void OnShieldPowerChanged(EntityUid uid, CircularShieldComponent shield, ref PowerChangedEvent args)
    {
        shield.Powered = args.Powered;
        
        if (shield.BoundConsole == null)
            return;
        SendShieldConsoleUpdates(shield.BoundConsole.Value);
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
