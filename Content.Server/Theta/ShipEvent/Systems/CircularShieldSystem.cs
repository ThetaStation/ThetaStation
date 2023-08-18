using System.Linq;
using System.Numerics;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Physics;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent.CircularShield;
using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class CircularShieldSystem : SharedCircularShieldSystem
{
    [Dependency] private readonly PhysicsSystem _physSys = default!;
    [Dependency] private readonly FixtureSystem _fixSys = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    private const string ShieldFixtureId = "ShieldFixture";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldToggleMessage>(OnShieldToggle);
        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldChangeParametersMessage>(OnShieldChangeParams);
        SubscribeLocalEvent<CircularShieldConsoleComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);

        SubscribeLocalEvent<CircularShieldComponent, ComponentInit>(OnShieldInit);
        SubscribeLocalEvent<CircularShieldComponent, PowerChangedEvent>(OnShieldPowerChanged);
        SubscribeLocalEvent<CircularShieldComponent, StartCollideEvent>(OnShieldFixtureEnter);
        SubscribeLocalEvent<CircularShieldComponent, EndCollideEvent>(OnShieldFixtureExit);
        SubscribeLocalEvent<CircularShieldComponent, NewLinkEvent>(OnShieldLink);
    }

    private void AfterUIOpen(EntityUid uid, CircularShieldConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateConsoleState(uid, component);
    }

    private void UpdateConsoleState(EntityUid uid, CircularShieldConsoleComponent? console = null, RadarConsoleComponent? radar = null)
    {
        if (!Resolve(uid, ref console, ref radar) || console.BoundShield == null)
            return;
        if (!TryComp<CircularShieldComponent>(console.BoundShield, out var shield) ||
            !TryComp<TransformComponent>(console.BoundShield, out var transform))
            return;

        var shieldState = new ShieldInterfaceState
        {
            Coordinates = _transformSystem.GetMoverCoordinates(console.BoundShield.Value, transform),
            WorldRotation = _transformSystem.GetWorldRotation(transform),
            Powered = shield.Powered,
            Angle = shield.Angle,
            Width = shield.Width,
            MaxWidth = shield.MaxWidth,
            Radius = shield.Radius,
            MaxRadius = shield.MaxRadius,
            IsControlling = true
        };

        var angle = Angle.Zero;
        _uiSys.TrySetUiState(uid, CircularShieldConsoleUiKey.Key, new ShieldConsoleBoundsUserInterfaceState(
            radar.MaxRange,
            transform.Coordinates,
            angle,
            shieldState
            ));
    }

    private void OnShieldToggle(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldToggleMessage args)
    {
        if(console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        shield.Enabled = !shield.Enabled;
        UpdateConsoleState(uid, console);

        if (shield.Enabled)
        {
            UpdateShieldFixture(console.BoundShield.Value, shield);
        }
        Dirty(shield);
    }

    private void OnShieldChangeParams(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldChangeParametersMessage args)
    {
        if(console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        if ((args.Radius != null && args.Radius > shield.MaxRadius) || args.Width?.Degrees > shield.MaxWidth)
            return;

        shield.Angle = args.Angle;
        if(args.Width != null)
            shield.Width = args.Width.Value;
        if(args.Radius != null)
            shield.Radius = args.Radius.Value;
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
        UpdateConsoleState(shield.BoundConsole.Value);
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

        Fixture? shieldFix = _fixSys.GetFixtureOrNull(uid, ShieldFixtureId);
        if (shieldFix == null)
        {
            PolygonShape shape = new PolygonShape();
            shape.Set(cone, cone.Length);

            _fixSys.TryCreateFixture(uid, shape, ShieldFixtureId, hard: false, collisionLayer: (int)CollisionGroup.BulletImpassable);
        }
        else
        {
            _physSys.SetVertices(uid, shieldFix, (PolygonShape)shieldFix.Shape, cone);
        }
    }
}
