using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Shuttles.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Preferences;
using Content.Shared.Theta.Impostor.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events.Theta;

[RegisterComponent, Access(typeof(ImpostorRuleSystem))]
public sealed partial class ImpostorRuleComponent : Component
{
    [DataField("impostorAntagId", required: true)] public string ImpostorAntagId;
    [DataField("impostorObjectiveGroupId", required: true)] public string ImpostorObjectiveGroupId;
    [DataField("impostorAmount")] public int ImpostorAmount = 1;
    [DataField("impostorGreetingSound")] public SoundSpecifier ImpostorGreetingSound;
}

public sealed class ImpostorRuleSystem : StationEventSystem<ImpostorRuleComponent>
{
    [Dependency] private MindSystem _mindSys = default!;
    [Dependency] private JobSystem _jobSys = default!;
    [Dependency] private RoleSystem _roleSys = default!;
    [Dependency] private ObjectivesSystem _objectiveSys = default!;
    [Dependency] private ChatSystem _chatSys = default!;
    [Dependency] private AudioSystem _audioSys = default!;
    [Dependency] private IRobustRandom _rand = default!;
    
    private ImpostorRuleComponent ruleComp = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerSpawning);
    }
    
    protected override void Started(EntityUid uid, ImpostorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        ruleComp = component;
    }

    private void OnPlayerSpawning(RulePlayerJobsAssignedEvent ev)
    {
        if (ruleComp == null) return;
        
        List<IPlayerSession> candidates = GetPotentialImpostors(ev.Players, ev.Profiles);
        for (int i = 0; i < ruleComp.ImpostorAmount; i++)
        {
            MakeImpostor(_rand.Pick(candidates));
        }
    }

    private void MakeImpostor(IPlayerSession player)
    {
        if (_mindSys.TryGetMind(player.UserId, out var mindUid, out var mind))
        {
            _roleSys.MindAddRole(mindUid.Value, new ImpostorRoleComponent());
            //_mindSys.AddObjective(mindUid.Value, mind, _objectiveSys.GetRandomObjective(mindUid.Value, mind, ruleComp.ImpostorObjectiveGroupId) ?? EntityUid.Invalid);
            _chatSys.SendSimpleMessage(Loc.GetString("impostor-greeting"), player);
            _audioSys.PlayGlobal(ruleComp.ImpostorGreetingSound, player);
        }
    }

    private List<IPlayerSession> GetPotentialImpostors(IPlayerSession[] playerPool, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        List<IPlayerSession> validPlayers = new();
        EntityQuery<PendingClockInComponent> pendingPlayers = GetEntityQuery<PendingClockInComponent>();
        foreach (IPlayerSession player in playerPool)
        {
            if (!_jobSys.CanBeAntag(player))
                continue;
            
            if (player.AttachedEntity != null && pendingPlayers.HasComponent(player.AttachedEntity.Value))
                continue;

            validPlayers.Add(player);
        }

        List<IPlayerSession> playersWithPref = new();
        foreach (IPlayerSession player in validPlayers)
        {
            if (!profiles.ContainsKey(player.UserId))
                continue;
            
            if (profiles[player.UserId].AntagPreferences.Contains(ruleComp.ImpostorAntagId))
                playersWithPref.Add(player);
        }
        if (playersWithPref.Count < ruleComp.ImpostorAmount)
        {
            Log.Info("Insufficient preferred impostors, picking at random.");
            return validPlayers;
        }
        
        return playersWithPref;
    }
}
