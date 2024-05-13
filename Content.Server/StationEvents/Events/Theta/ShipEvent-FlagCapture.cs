using System.Threading;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Theta.MapGen;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Roles.Theta;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events.Theta;


[RegisterComponent, Access(typeof(SEFCRule))]
public sealed partial class SEFCRuleComponent : Component { } //yes partial is needed here

[RegisterComponent]
public sealed partial class SEFCFlagComponent : Component
{
    /// <summary>
    /// Last team which has captured this flag
    /// </summary>
    public ShipEventTeam? LastTeam;

    /// <summary>
    /// To prevent message spam
    /// </summary>
    public TimeSpan LastParentChangeTime;
}

//Ship event flag capture
public sealed class SEFCRule : StationEventSystem<SEFCRuleComponent>
{
    [Dependency] private TransformSystem _formSys = default!;
    [Dependency] private MapGenSystem _mapGenSys = default!;
    [Dependency] private ShipEventTeamSystem _shipSys = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const string FlagPrototypeId = "SEFCFlag";
    private const int PointsPerFlag = (int) 1E6;
    private const int UpdateInterval = 10;

    private Box2 FieldBounds => _shipSys.GetPlayAreaBounds(); //fetching it multiple times since on start it's a single point + it might compress

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SEFCFlagComponent, EntParentChangedMessage>(OnFlagParentChanged);
        SubscribeLocalEvent<SEFCFlagComponent, ComponentShutdown>(OnFlagShutdown);
        _shipSys.RoundEndEvent += OnRoundEnd;
    }

    protected override void Started(EntityUid uid, SEFCRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!_shipSys.RuleSelected)
        {
            Log.Warning("Tried to start SEFC without shipevent, exiting.");
            return;
        }

        Box2 fieldBounds = _shipSys.GetPlayAreaBounds();
        _mapGenSys.ClearArea(_shipSys.TargetMap, (Box2i) new Box2(fieldBounds.BottomLeft, fieldBounds.TopRight).Scale(0.1f));
        Spawn(FlagPrototypeId, new MapCoordinates(fieldBounds.Center, _shipSys.TargetMap));

        _shipSys.CreateTeam(default!, "RED", null, null, 0, true);
        _shipSys.Teams[0].Color = Color.Red;

        _shipSys.CreateTeam(default!, "BLU", null, null, 0, true);
        _shipSys.Teams[1].Color = Color.Blue;

        _shipSys.AllowTeamRegistration = false;
        _shipSys.RemoveEmptyTeams = false;

        Timer.SpawnRepeating(UpdateInterval, CheckFlagPositions, CancellationToken.None);
    }

    //to ensure that flags will not become stuck inside some asteroid
    private void ClearCenterOfTheField()
    {
        _mapGenSys.ClearArea(_shipSys.TargetMap, (Box2i) new Box2(FieldBounds.BottomLeft, FieldBounds.TopRight).Scale(0.1f));
    }

    private void OnFlagParentChanged(EntityUid uid, SEFCFlagComponent flag, ref EntParentChangedMessage args)
    {
        bool parentChangedRecently = (_timing.CurTime - flag.LastParentChangeTime).TotalSeconds < 1.0;
        flag.LastParentChangeTime = _timing.CurTime;
        if (parentChangedRecently)
            return;

        ShipEventTeam? oldTeam = CompOrNull<ShipEventTeamMarkerComponent>(args.OldParent)?.Team;
        ShipEventTeam? newTeam = CompOrNull<ShipEventTeamMarkerComponent>(args.Transform.ParentUid)?.Team;

        if (oldTeam != null)
            _shipSys.TeamMessage(oldTeam, Loc.GetString("sefc-flaglost"));
        if (newTeam != null)
        {
            _shipSys.TeamMessage(newTeam, Loc.GetString("sefc-flagrecovered"));
            flag.LastTeam = newTeam;
        }
    }

    private void OnFlagShutdown(EntityUid uid, SEFCFlagComponent flag, ComponentShutdown args)
    {
        if (!_shipSys.RuleSelected) //it shouldn't get this far without SE rule selected anyway, this is just for the unit tests
            return;

        ClearCenterOfTheField();
        EntityUid newFlag = Spawn(FlagPrototypeId, new MapCoordinates(FieldBounds.Center, _shipSys.TargetMap));
        Comp<SEFCFlagComponent>(newFlag).LastTeam = flag.LastTeam;
    }

    private void CheckFlagPositions()
    {
        bool centerClear = false;

        foreach ((SEFCFlagComponent _, TransformComponent form) in EntityManager.EntityQuery<SEFCFlagComponent, TransformComponent>())
        {
            if (!FieldBounds.Contains(_formSys.GetWorldPosition(form)))
            {
                if (!centerClear)
                {
                    ClearCenterOfTheField();
                    centerClear = true;
                }
                _formSys.SetWorldPosition(form, FieldBounds.Center);
            }
        }
    }

    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
        var query = EntityManager.EntityQueryEnumerator<SEFCFlagComponent>();
        while (query.MoveNext(out var uid, out var flag))
        {
            if (flag.LastTeam == null)
            {
                Log.Error($"SEFC, OnRoundEnd: Flag's last team is null ({uid}). Either none of the teams have ever captured it, or something went wrong.");
                continue;
            }

            flag.LastTeam.Points += PointsPerFlag;
            args.AddLine(Loc.GetString("sefc-teamwin", ("team", flag.LastTeam.Name)));
        }
    }
}
