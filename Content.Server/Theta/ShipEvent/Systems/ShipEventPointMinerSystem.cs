using Content.Server.DoAfter;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;
using Content.Server.Theta.RadarRenderable;
using Content.Shared.Theta.ShipEvent.Misc;
using Content.Shared.Roles.Theta;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class ShipEventPointMinerSystem : EntitySystem
{
    [Dependency] private readonly ShipEventTeamSystem _shipSys = default!;
    [Dependency] private readonly ITimerManager _timerMan = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSys = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSys = default!;
    [Dependency] private readonly SharedAudioSystem _audioSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipEventPointMinerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ShipEventPointMinerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShipEventPointMinerComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<ShipEventPointMinerComponent, ShipEventPointMinerOverrideFinished>(OnOverrideFinished);
    }

    private void OnStartup(EntityUid uid, ShipEventPointMinerComponent miner, ref ComponentStartup args)
    {
        SetupTimer(uid, miner);
    }

    private void OnShutdown(EntityUid uid, ShipEventPointMinerComponent miner, ref ComponentShutdown args)
    {
        CancelTimer(miner);
    }

    private void OnHandInteract(EntityUid uid, ShipEventPointMinerComponent miner, ref InteractHandEvent args)
    {
        if (!TryComp<ShipEventTeamMarkerComponent>(args.User, out var marker) || marker.Team == null)
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            miner.OverrideDelay,
            new ShipEventPointMinerOverrideFinished() { Team = marker.Team.Name },
            uid,
            uid,
            null)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };
        _doAfterSys.TryStartDoAfter(doAfterArgs);
    }

    private void OnOverrideFinished(EntityUid uid, ShipEventPointMinerComponent miner, ref ShipEventPointMinerOverrideFinished args)
    {
        ShipEventTeam? targetTeam = null;
        foreach (ShipEventTeam team in _shipSys.Teams)
        {
            if (team.Name == args.Team)
            {
                targetTeam = team;
                break;
            }
        }

        if (targetTeam == null)
            return;

        miner.OwnerTeam = targetTeam;

        if (TryComp<PointLightComponent>(uid, out var light))
            _lightSys.SetColor(uid, targetTeam.Color, light);

        if (TryComp<RadarRenderableComponent>(uid, out var rend))
            rend.OverrideColor = targetTeam.Color;
    }

    private void OnTimerFire(EntityUid uid, ShipEventPointMinerComponent miner)
    {
        if (miner.OwnerTeam == null)
            return;

        miner.OwnerTeam.Points += miner.PointsPerInterval;
        _audioSys.PlayPredicted(miner.FireSound, uid, uid);
    }

    private void SetupTimer(EntityUid uid, ShipEventPointMinerComponent miner)
    {
        _timerMan.AddTimer(new Timer(miner.Interval * 1000, true, () => { OnTimerFire(uid, miner); }), miner.TimerTokenSource.Token);
    }

    private void CancelTimer(ShipEventPointMinerComponent miner)
    {
        miner.TimerTokenSource.Cancel();
        miner.TimerTokenSource.Dispose();
        miner.TimerTokenSource = new();
    }

    public void SetMinerInterval(EntityUid uid, ShipEventPointMinerComponent miner, int newInterval)
    {
        miner.Interval = newInterval;
        CancelTimer(miner);
        SetupTimer(uid, miner);
    }
}