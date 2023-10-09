using System.Numerics;
using System.Threading;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Theta.DebrisGeneration;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Roles.Theta;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
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
    public ShipEventFaction? LastTeam;
}

//Ship event flag capture
public sealed class SEFCRule : StationEventSystem<SEFCRuleComponent>
{
    [Dependency] private TransformSystem _formSys = default!;
    [Dependency] private DebrisGenerationSystem _debrisSys = default!;
    [Dependency] private ShipEventFactionSystem _shipSys = default!;

    private const string FlagPrototypeId = "SEFCFlag";
    private const int PointsPerFlag = (int)1E6;
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
        
        Box2 fieldBounds = _shipSys.GetPlayAreaBounds();
        _debrisSys.ClearArea(_shipSys.TargetMap, (Box2i)new Box2(fieldBounds.BottomLeft, fieldBounds.TopRight).Scale(0.1f));
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
        _debrisSys.ClearArea(_shipSys.TargetMap, (Box2i)new Box2(FieldBounds.BottomLeft, FieldBounds.TopRight).Scale(0.1f));
    }

    private void OnFlagParentChanged(EntityUid uid, SEFCFlagComponent flag, ref EntParentChangedMessage args)
    {
        ShipEventFaction? oldTeam = CompOrNull<ShipEventFactionMarkerComponent>(args.OldParent)?.Team;
        ShipEventFaction? newTeam = CompOrNull<ShipEventFactionMarkerComponent>(args.Transform.ParentUid)?.Team;
        if(oldTeam != null)
            _shipSys.TeamMessage(oldTeam, Loc.GetString("sefc-flaglost"));
        if (newTeam != null)
        {
            _shipSys.TeamMessage(newTeam, Loc.GetString("sefc-flagrecovered"));
            flag.LastTeam = newTeam;
        }
    }
    
    private void OnFlagShutdown(EntityUid uid, SEFCFlagComponent flag, ComponentShutdown args)
    {
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
        foreach (SEFCFlagComponent flag in EntityManager.EntityQuery<SEFCFlagComponent>())
        {
            if (flag.LastTeam == null)
            {
                Log.Error($"SEFC, OnRoundEnd: Flag's last team is null ({flag.Owner}). Either none of the teams have ever captured it, or something went wrong.");
                continue;
            }
            
            flag.LastTeam.Points += PointsPerFlag;
            args.AddLine(Loc.GetString("sefc-teamwin", ("team", flag.LastTeam.Name)));
        }
    }
}
