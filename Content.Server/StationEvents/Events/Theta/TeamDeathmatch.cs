using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Theta.TeamDeathmatch;

namespace Content.Server.StationEvents.Events.Theta;

[RegisterComponent, Access(typeof(TeamDeathmatchRule))]
public sealed class TeamDeathmatchRuleComponent : Component
{
    [DataField("respawnDelay")] public int RespawnDelay;

    [DataField("bonusKillsForObelisk")] public int BonusKillsForObelisk;

    [DataField("gunPrototypes")] public List<string> GunPrototypes = new();
}

public sealed class TeamDeathmatchRule : GameRuleSystem<TeamDeathmatchRuleComponent>
{
    [Dependency] private TeamDeathmatchSystem _tdmSys = default!;

    protected override void Started(EntityUid uid, TeamDeathmatchRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        _tdmSys.RuleSelected = true;
        base.Started(uid, component, gameRule, args);
        _tdmSys.RespawnDelay = component.RespawnDelay;
        _tdmSys.BonusKillsForObelisk = component.BonusKillsForObelisk;
        _tdmSys.GunPrototypes = component.GunPrototypes;
    }
}
