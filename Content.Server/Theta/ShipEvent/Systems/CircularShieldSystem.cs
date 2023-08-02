using System.Linq;
using System.Numerics;
using Content.Server.Power.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
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
        
        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldToggleMessage>(OnShieldToggle);
        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldChangeParametersMessage>(OnShieldChangeParams);
        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldConsoleInfoRequest>(OnConsoleInfoRequest);

        SubscribeLocalEvent<CircularShieldComponent, ComponentInit>(OnShieldInit);
        SubscribeLocalEvent<CircularShieldComponent, PowerChangedEvent>(OnShieldPowerChanged);
        SubscribeLocalEvent<CircularShieldComponent, StartCollideEvent>(OnShieldFixtureEnter);
        SubscribeLocalEvent<CircularShieldComponent, EndCollideEvent>(OnShieldFixtureExit);
        SubscribeLocalEvent<CircularShieldComponent, NewLinkEvent>(OnShieldLink);
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
    
    private void OnShieldToggle(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldToggleMessage args)
    {
        if(console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        shield.Enabled = !shield.Enabled;
    }

    private void OnShieldChangeParams(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldChangeParametersMessage args)
    {
        if(console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        if (args.Radius > shield.MaxRadius || args.Width.Degrees > shield.MaxWidth)
            return;

        shield.Angle = args.Angle;
        shield.Width = args.Width;
        shield.Radius = args.Radius;
        UpdateShieldFixture(console.BoundShield.Value, shield);

        if (TryComp<ApcPowerReceiverComponent>(console.BoundShield.Value, out var receiver))
        {
            receiver.Load = shield.DesiredDraw;
        }
        else
        {
            if(shield.DesiredDraw > 0)
                shield.Powered = false;
        }
        
        Dirty(shield);
    }

    private void OnShieldInit(EntityUid uid, CircularShieldComponent shield, ComponentInit args)
    {
        if (EntityManager.TryGetComponent<DeviceLinkSinkComponent>(uid, out var source))
        {
            if (source.LinkedSources.Count > 0)
            {
                EntityUid consoleUid = source.LinkedSources.First();
                if (TryComp<CircularShieldConsoleComponent>(consoleUid, out var console))
                {
                    console.BoundShield = uid;
                    shield.BoundConsole = consoleUid;
                }
            }
        }
        
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
        Dirty(shield);
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
    
    private void OnShieldLink(EntityUid uid, CircularShieldComponent shield, NewLinkEvent args)
    {
        if (!TryComp<CircularShieldConsoleComponent>(args.Source, out var console))
            return;

        shield.BoundConsole = args.Source;
        console.BoundShield = uid;
        
        Dirty(shield);
        Dirty(console);
    }

    private void UpdateShieldFixture(EntityUid uid, CircularShieldComponent shield, int extraArcPoints = 0)
    {
        if (shield.Radius < 1)
            shield.Radius = 1;
        if (shield.Width < 0.16) //10 deg
            shield.Width = 0.16;

        Vector2[] cone = GenerateConeVertices(shield.Radius, shield.Angle, shield.Width, 5);

        Fixture? shieldFix = fixSys.GetFixtureOrNull(uid, ShieldFixtureId);
        if (shieldFix == null)
        {
            PolygonShape shape = new PolygonShape();
            shape.Set(cone, cone.Length);

            fixSys.TryCreateFixture(uid, shape, ShieldFixtureId, hard: false, collisionLayer: (int)CollisionGroup.BulletImpassable);
        }
        else
        {
            physSys.SetVertices(uid, shieldFix, (PolygonShape)shieldFix.Shape, cone);
        }
    }

    private Vector2[] GenerateConeVertices(int radius, Angle angle, Angle width, int extraArcPoints = 0)
    {
        Vector2[] vertices = new Vector2[3 + extraArcPoints];
        vertices[0] = new Vector2(0, 0);

        Angle start = angle - width / 2;
        Angle step = width / (2 + extraArcPoints);
        
        for (int i = 1; i < 3 + extraArcPoints; i++)
        {
            vertices[i] = (start + step * (i - 1)).ToVec() * radius;
        }

        return vertices;
    }
}
