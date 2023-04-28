using Content.Server.Power.Components;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class BluespaceCatapultSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSys = default!;
    [Dependency] private readonly TransformSystem _formSys = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    [Dependency] private readonly SharedAudioSystem _audioSys = default!;

    private const int minMass = 10;
    private const int maxDistance = 10000;
    private const int fallAcceleration = 10;
    private const float animationLength = 1.2f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BluespaceCatapultComponent, ComponentInit>(SetupCatapult);
        SubscribeLocalEvent<BluespaceCatapultComponent, BluespaceCatapultLaunchRequest>(OnLaunchRequest);
        SubscribeLocalEvent<BluespaceCatapultComponent, BluespaceCatapultRefreshRequest>(OnRefreshRequest);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (IComponent catapultComp in EntityManager.GetAllComponents(typeof(BluespaceCatapultComponent)))
        {
            if (catapultComp is BluespaceCatapultComponent catapult)
            {
                if (catapult.animationTimer != 0)
                {
                    catapult.animationTimer -= frameTime;
                    if (catapult.animationTimer <= 0)
                    {
                        catapult.animationTimer = 0;
                        if (EntityManager.TryGetComponent<SpriteComponent>(catapult.Owner, out var sprite))
                            sprite.LayerSetState(0, "catapult");
                    }
                }

                if (!catapult.IsFullyCharged)
                {
                    if (catapult.Consumer != null)
                    {
                        if(catapult.Consumer.DrawRate == 0)
                            catapult.Consumer.DrawRate = catapult.ChargeRate;

                        if (catapult.Battery != null)
                            catapult.Battery.CurrentCharge += catapult.Consumer.ReceivedPower;
                    }
                }
                else
                {
                    if (catapult.Consumer != null)
                        catapult.Consumer.DrawRate = 0;
                }
            }
        }
    }

    private void SetupCatapult(EntityUid uid, BluespaceCatapultComponent catapult, ComponentInit args)
    {
        catapult.Consumer = EntityManager.EnsureComponent<PowerConsumerComponent>(uid);
        catapult.Battery = EntityManager.EnsureComponent<BatteryComponent>(uid);
        if (catapult.Battery.MaxCharge == 0)
            catapult.Battery.MaxCharge = catapult.MaxPower;
    }

    private void OnLaunchRequest(EntityUid uid, BluespaceCatapultComponent catapult, BluespaceCatapultLaunchRequest args)
    {
        if (catapult.Charge < args.Power)
        {
            UpdateUI(uid, catapult.Charge, catapult.MaxCharge, Loc.GetString("shipevent-bluespacecatapult-response-lowpower"));
            return;
        }

        if (catapult.MaxPower < args.Power || args.Power < 0 ||
            360 < args.Bearing || args.Bearing < 0 || 
            360 < args.Elevation || args.Elevation < 0)
        {
            UpdateUI(uid, catapult.Charge, catapult.MaxCharge, Loc.GetString("shipevent-bluespacecatapult-response-invaliddata"));
            return;
        }
        
        HashSet<EntityUid> launchedObjects = _lookup.GetEntitiesInRange(new EntityCoordinates(uid, Vector2.Zero), 0.5f, LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries);

        float mass = 0;
        foreach (EntityUid launchedUid in launchedObjects)
        {
            if (Transform(launchedUid).Anchored)
            {
                launchedObjects.Remove(launchedUid);
                continue;
            }

            if (EntityManager.TryGetComponent<PhysicsComponent>(launchedUid, out var phys))
                mass += phys.Mass;
        }

        if (mass < minMass) //to not send some lightweight garbage gajillion kilometers away
        {
            UpdateUI(uid, catapult.Charge, catapult.MaxCharge, Loc.GetString("shipevent-bluespacecatapult-response-lowmass"));
            return;
        }

        catapult.Battery!.UseCharge(args.Power);

        float velocity = args.Power * catapult.Efficiency / mass;
        Angle elevation = Angle.FromDegrees(args.Elevation + _rand.Next(-catapult.MaxError, catapult.MaxError));
        Angle bearing = Angle.FromDegrees(args.Bearing + _rand.Next(-catapult.MaxError, catapult.MaxError));
        float distance = (float)Math.Clamp(velocity * velocity * Math.Sin(elevation.Theta * 2) / (2 * fallAcceleration), 0, maxDistance);

        foreach (EntityUid launchedUid in launchedObjects)
        {
            var form = Transform(launchedUid);
            form.Coordinates = form.Coordinates.Offset(bearing.ToVec() * distance);
        }

        _audioSys.PlayPvs(catapult.LaunchSound, uid, AudioParams.Default);

        //this is really bad, but I don't want to create client side system for a single animation
        catapult.animationTimer = animationLength;
        if (EntityManager.TryGetComponent<SpriteComponent>(uid, out var sprite))
            sprite.LayerSetState(0, "catapult-launch");
    }
    
    private void OnRefreshRequest(EntityUid uid, BluespaceCatapultComponent catapult, BluespaceCatapultRefreshRequest args)
    {
        UpdateUI(uid, catapult.Charge, catapult.MaxCharge, "");
    }

    private void UpdateUI(EntityUid uid, float charge, float maxCharge, string message)
    {
        _uiSys.TrySetUiState(uid, BluespaceCatapultUiKey.Key, new BluespaceCatapultBoundUserInterfaceState(charge, maxCharge, message));
    }
}
