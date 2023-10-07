using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Theta.ShipEvent.Components;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.Roles.Theta;

namespace Content.Server.StationEvents.Events.Theta;


[RegisterComponent, Access(typeof(SEFCRule))]
public sealed partial class SEFCRuleComponent : Component { } //yes partial is needed here

[RegisterComponent]
public sealed partial class SEFCFlagComponent : Component
{
    /// <summary>
    /// Last team which has captured this flag
    /// </summary>
    public ShipEventFaction LastTeam = default!;
}

//Ship event flag capture
public sealed class SEFCRule : StationEventSystem<SEFCRuleComponent>
{
    [Dependency] private ShipEventFactionSystem _shipSys = default!;
    private const int PointsPerFlag = (int)10E6;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SEFCFlagComponent, EntParentChangedMessage>(OnFlagParentChanged);
        _shipSys.RoundEndEvent += OnRoundEnd;
    }

    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
        foreach (SEFCFlagComponent flag in EntityManager.EntityQuery<SEFCFlagComponent>())
        {
            if (flag.LastTeam == null)
            {
                Log.Error($"SEFC: Flag's last team is null ({flag.Owner}). Either none of the teams have ever captured it, or something went wrong.");
                continue;
            }
            
            flag.LastTeam.Points += PointsPerFlag;
            args.AddLine(Loc.GetString("sefc-teamwin", ("team", flag.LastTeam.Name)));
        }
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

    protected override void Started(EntityUid uid, SEFCRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        
        _shipSys.CreateTeam(default!, "RED", null, null, 0, true);
        _shipSys.Teams[0].Color = Color.Red;
        
        _shipSys.CreateTeam(default!, "BLU", null, null, 0, true);
        _shipSys.Teams[1].Color = Color.Blue;
        
        _shipSys.AllowTeamRegistration = false;
        _shipSys.RemoveEmptyTeams = false;
    }
}
