using Content.Server.GameTicking.Rules.Components;
using Content.Server.Theta.ShipEvent.Systems;

namespace Content.Server.StationEvents.Events.Theta;


[RegisterComponent, Access(typeof(SEFCRule))]
public sealed partial class SEFCRuleComponent : Component { } //yes partial is needed here

[RegisterComponent]
public sealed partial class SEFCFlagComponent : Component { }

//Ship event flag capture
public sealed class SEFCRule : StationEventSystem<SEFCRuleComponent>
{
    [Dependency] private ShipEventFactionSystem _shipSys = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SEFCFlagComponent, EntParentChangedMessage>(OnFlagParentChanged);
    }

    private void OnFlagParentChanged(EntityUid uid, SEFCFlagComponent flag, ref EntParentChangedMessage args)
    {
    }

    protected override void Started(EntityUid uid, SEFCRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _shipSys.CreateTeam(default!, "RED", null, null, 0, true);
        _shipSys.CreateTeam(default!, "BLU", null, null, 0, true);
        _shipSys.AllowTeamRegistration = false;
        _shipSys.RemoveEmptyTeams = false;
    }
}
