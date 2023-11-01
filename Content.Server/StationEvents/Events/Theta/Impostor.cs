using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Shuttles.Components;
using Content.Server.Theta.Impostor.Components;
using Content.Server.Theta.Impostor.Systems;
using Content.Shared.Objectives.Components;
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
    [DataField("impostorAntagId", required: true)] public string ImpostorAntagId = "";
    [DataField("impostorMandatoryObjectives", required: true)] public List<string> ImpostorMandatoryObjectives = default!;
    [DataField("impostorRandomObjectivesGroupId")] public string? ImpostorRandomObjectivesGroupId;
    [DataField("impostorAmount")] public int ImpostorAmount = 1;
    [DataField("randomObjectiveAmount")] public int RandomObjectiveAmount;
    [DataField("evacTriggerDeathCount", required: true)] public int EvacTriggerDeathCount;
    [DataField("evacLaunchDelay", required: true)] public int EvacLaunchDelay; //in minutes
    [DataField("impostorGreetingSound")] public SoundSpecifier? ImpostorGreetingSound;
}

public sealed partial class ImpostorRuleSystem : StationEventSystem<ImpostorRuleComponent>
{
    [Dependency] private MindSystem _mindSys = default!;
    [Dependency] private JobSystem _jobSys = default!;
    [Dependency] private RoleSystem _roleSys = default!;
    [Dependency] private ObjectivesSystem _objectiveSys = default!;
    [Dependency] private ChatSystem _chatSys = default!;
    [Dependency] private AudioSystem _audioSys = default!;
    [Dependency] private IRobustRandom _rand = default!;
    [Dependency] private ImpostorEvacSystem _specEvacSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerSpawning);
    }
    
    protected override void Started(EntityUid uid, ImpostorRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        _specEvacSys.RuleSelected = true;
        _specEvacSys.TriggerDeathCount = component.EvacTriggerDeathCount;
        _specEvacSys.LaunchDelay = component.EvacLaunchDelay;
    }

    private void OnPlayerSpawning(RulePlayerJobsAssignedEvent ev)
    {
        ImpostorRuleComponent rule = EntityQuery<ImpostorRuleComponent>().First();
        
        List<IPlayerSession> candidates = GetPotentialImpostors(ev.Players, ev.Profiles);
        for (int i = 0; i < rule.ImpostorAmount; i++)
        {
            MakeImpostor(_rand.Pick(candidates));
        }
    }

    private void MakeImpostor(IPlayerSession player)
    {
        ImpostorRuleComponent rule = EntityQuery<ImpostorRuleComponent>().First();
        
        if (_mindSys.TryGetMind(player.UserId, out var mindUid, out var mind))
        {
            _roleSys.MindAddRole(mindUid.Value, new ImpostorRoleComponent());

            foreach (string mandatoryObjective in rule.ImpostorMandatoryObjectives)
            {
                EntityUid? objectiveUid = _objectiveSys.TryCreateObjective(mindUid.Value, mind, mandatoryObjective);
                if(objectiveUid != null)
                    _mindSys.AddObjective(mindUid.Value, mind, objectiveUid.Value);
            }

            if (rule.ImpostorRandomObjectivesGroupId != null)
            {
                for (int i = 0; i < rule.RandomObjectiveAmount; i++)
                {
                    EntityUid? objective = _objectiveSys.GetRandomObjective(mindUid.Value, mind, rule.ImpostorRandomObjectivesGroupId);
                    if (objective == null) 
                        return;
                    _mindSys.AddObjective(mindUid.Value, mind, objective.Value);
                }
            }

            _chatSys.SendSimpleMessage(Loc.GetString("impostor-greeting"), player);
            _audioSys.PlayGlobal(rule.ImpostorGreetingSound, player);
        }
    }

    private List<IPlayerSession> GetPotentialImpostors(IPlayerSession[] playerPool, IReadOnlyDictionary<NetUserId, HumanoidCharacterProfile> profiles)
    {
        ImpostorRuleComponent rule = EntityQuery<ImpostorRuleComponent>().First();
        
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
            
            if (profiles[player.UserId].AntagPreferences.Contains(rule.ImpostorAntagId))
                playersWithPref.Add(player);
        }
        if (playersWithPref.Count < rule.ImpostorAmount)
        {
            Log.Info("Insufficient preferred impostors, picking at random.");
            return validPlayers;
        }
        
        return playersWithPref;
    }
}
