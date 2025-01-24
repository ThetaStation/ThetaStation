using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Physics;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent.CircularShield;
using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
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
    [Dependency] private readonly TransformSystem _formSys = default!;
    [Dependency] private readonly ShuttleConsoleSystem _shuttleConsole = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsIgnoreSys = default!;

    private const string ShieldFixtureId = "ShieldFixture";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldToggleMessage>(OnShieldToggle);
        SubscribeLocalEvent<CircularShieldConsoleComponent, CircularShieldChangeParametersMessage>(OnShieldChangeParams);
        SubscribeLocalEvent<CircularShieldConsoleComponent, AfterActivatableUIOpenEvent>(AfterUIOpen);

        SubscribeLocalEvent<CircularShieldConsoleComponent, ComponentInit>(OnShieldConsoleInit);
        SubscribeLocalEvent<CircularShieldComponent, ComponentShutdown>(OnShieldRemoved);
        SubscribeLocalEvent<CircularShieldComponent, PowerChangedEvent>(OnShieldPowerChanged);
        SubscribeLocalEvent<CircularShieldComponent, StartCollideEvent>(OnShieldEnter);
        SubscribeLocalEvent<CircularShieldComponent, NewLinkEvent>(OnShieldLink);
    }

    public override void Update(float time)
    {
        base.Update(time);
        var query = EntityManager.EntityQueryEnumerator<CircularShieldComponent>();
        while (query.MoveNext(out EntityUid uid, out CircularShieldComponent? shield))
        {
            foreach (CircularShieldEffect effect in shield.Effects)
            {
                effect.OnShieldUpdate(uid, shield, time);
            }
        }
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
            Coordinates = GetNetCoordinates(_formSys.GetMoverCoordinates(console.BoundShield.Value, transform)),
            Powered = shield.Powered,
            Angle = shield.Angle,
            Width = shield.Width,
            MaxWidth = shield.MaxWidth,
            Radius = shield.Radius,
            MaxRadius = shield.MaxRadius
        };

        _uiSys.SetUiState(uid, CircularShieldConsoleUiKey.Key, new ShieldConsoleBoundsUserInterfaceState(
            _shuttleConsole.GetNavState(uid, new Dictionary<NetEntity, List<DockingPortState>>()),
            shieldState
            ));
    }

    private void OnShieldToggle(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldToggleMessage args)
    {
        if (console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        shield.Enabled = !shield.Enabled;
        UpdatePowerDraw(uid, shield);
        UpdateConsoleState(uid, console);

        if (!shield.Enabled)
        {
            foreach (CircularShieldEffect effect in shield.Effects)
            {
                effect.OnShieldShutdown(uid, shield);
            }
        }

        Dirty(console.BoundShield.Value, shield);
    }

    private void OnShieldChangeParams(EntityUid uid, CircularShieldConsoleComponent console, CircularShieldChangeParametersMessage args)
    {
        if (console.BoundShield == null)
            return;

        if (!TryComp(console.BoundShield, out CircularShieldComponent? shield))
            return;

        if (args.Radius > shield.MaxRadius || args.Width?.Degrees > shield.MaxWidth)
            return;

        shield.Angle = args.Angle ?? shield.Angle;
        shield.Width = args.Width ?? shield.Width;
        shield.Radius = args.Radius ?? shield.Radius;

        UpdateShieldFixture(console.BoundShield.Value, shield);
        UpdatePowerDraw(console.BoundShield.Value, shield);

        Dirty(console.BoundShield.Value, shield);
    }

    //this is silly, but apparently sink component on shields does not contain linked sources on startup
    //while source component on consoles always does right after init
    //so subscribing to it instead of sink
    private void OnShieldConsoleInit(EntityUid uid, CircularShieldConsoleComponent console, ComponentInit args)
    {
        _pvsIgnoreSys.AddGlobalOverride(uid);

        EntityUid shieldUid;
        CircularShieldComponent shield;

        if (!TryComp<DeviceLinkSourceComponent>(uid, out var source))
            return;

        if (source.LinkedPorts.Count == 0)
            return;

        shieldUid = source.LinkedPorts.First().Key;
        shield = Comp<CircularShieldComponent>(shieldUid);
        console.BoundShield = shieldUid;
        shield.BoundConsole = uid;

        foreach (CircularShieldEffect effect in shield.Effects)
        {
            effect.OnShieldInit(uid, shield);
        }
    }

    private void OnShieldRemoved(EntityUid uid, CircularShieldComponent shield, ComponentShutdown args)
    {
        foreach (CircularShieldEffect effect in shield.Effects)
        {
            effect.OnShieldShutdown(uid, shield);
        }
    }

    private void OnShieldPowerChanged(EntityUid uid, CircularShieldComponent shield, ref PowerChangedEvent args)
    {
        shield.Powered = args.Powered;

        if (shield.BoundConsole == null)
            return;
        UpdateConsoleState(shield.BoundConsole.Value);

        if (!shield.Powered)
        {
            foreach (CircularShieldEffect effect in shield.Effects)
            {
                effect.OnShieldShutdown(uid, shield);
            }
        }

        Dirty(uid, shield);
    }

    private void OnShieldEnter(EntityUid uid, CircularShieldComponent shield, ref StartCollideEvent args)
    {
        if (!shield.CanWork || args.OurFixtureId != ShieldFixtureId)
            return;

        if (!EntityInShield(uid, shield, args.OtherEntity, _formSys))
            return;

        foreach (CircularShieldEffect effect in shield.Effects)
        {
            effect.OnShieldEnter(args.OtherEntity, shield);
        }
    }

    private void OnShieldLink(EntityUid uid, CircularShieldComponent shield, NewLinkEvent args)
    {
        if (!TryComp<CircularShieldConsoleComponent>(args.Source, out var console))
            return;

        shield.BoundConsole = args.Source;
        console.BoundShield = uid;

        Dirty(uid, shield);
        Dirty(shield.BoundConsole.Value, console);
    }

    private void UpdateShieldFixture(EntityUid uid, CircularShieldComponent shield)
    {
        shield.Radius = Math.Max(shield.Radius, 1);
        shield.Width = Math.Max(shield.Width, Angle.FromDegrees(10));

        Fixture? shieldFix = _fixSys.GetFixtureOrNull(uid, ShieldFixtureId);
        if (shieldFix == null)
        {
            PhysShapeCircle circle = new(shield.Radius);
            _fixSys.TryCreateFixture(uid, circle, ShieldFixtureId, hard: false, collisionLayer: (int) CollisionGroup.BulletImpassable);
        }
        else
        {
            _physSys.SetRadius(uid, ShieldFixtureId, shieldFix, shieldFix.Shape, shield.Radius);
        }
    }

    private void UpdatePowerDraw(EntityUid uid, CircularShieldComponent shield)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
        {
            receiver.Load = shield.DesiredDraw;
        }
        else if (shield.DesiredDraw > 0)
        {
            shield.Powered = false;
        }
    }
}
