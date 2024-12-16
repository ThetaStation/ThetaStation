using Content.Server.GameTicking;
using Content.Server.Theta.ShipEvent.Systems;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events.Theta;


[RegisterComponent, Access(typeof(SETRule))]
public sealed partial class SETRuleComponent : Component { }

//Ship event tactical mode
public sealed class SETRule : StationEventSystem<SETRuleComponent>
{
    [Dependency] private ShipEventTeamSystem _shipSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        _shipSys.RoundEndEvent += OnRoundEnd;
    }

    protected override void Started(EntityUid uid, SETRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!_shipSys.RuleSelected)
        {
            Log.Warning("Tried to start SET without shipevent, exiting.");
            return;
        }
    }

    private void OnRoundEnd(RoundEndTextAppendEvent args)
    {
    }
}
