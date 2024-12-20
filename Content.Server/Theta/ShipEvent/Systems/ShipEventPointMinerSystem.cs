using Content.Server.DoAfter;
using Content.Server.Theta.ShipEvent.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;
using Content.Server.Theta.RadarRenderable;
using Content.Shared.Theta.ShipEvent.Misc;
using Content.Shared.Roles.Theta;
using Robust.Shared.Timing;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed class ShipEventPointMinerSystem : EntitySystem
{
    [Dependency] private readonly ShipEventTeamSystem _shipSys = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSys = default!;
    [Dependency] private readonly SharedPointLightSystem _lightSys = default!;
    [Dependency] private readonly SharedAudioSystem _audioSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShipEventPointMinerComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<ShipEventPointMinerComponent, ShipEventPointMinerOverrideFinished>(OnOverrideFinished);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShipEventPointMinerComponent>();
        while (query.MoveNext(out var uid, out var miner))
        {
            if (miner.NextFire == null && miner.OwnerTeam != null)
            {
                miner.NextFire = _timing.CurTime + miner.Interval;
                continue;
            }

            if (miner.NextFire <= _timing.CurTime)
            {
                OnTimerFire(uid, miner);
                miner.NextFire = miner.NextFire.Value + miner.Interval;
            }
        }
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
}