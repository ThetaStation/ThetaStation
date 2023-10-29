using System.Numerics;
using System.Threading;
using Content.Server.Chat.Systems;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Theta.Impostor.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Objectives.Components;
using Content.Shared.Theta.Impostor.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Theta.Impostor.Systems;

//tldr: default evac system is shit
public sealed class ImpostorEvacSystem : EntitySystem
{
    public bool RuleSelected;
    
    /// <summary>
    /// True if evacuation is happening right now
    /// </summary>
    public bool EvacActive;

    /// <summary>
    /// True if evacuation has already happened
    /// </summary>
    public bool EvacFinished;

    public int TriggerDeathCount;

    public int CurrentDeathCount;

    /// <summary>
    /// In minutes
    /// </summary>
    public int LaunchDelay;

    private CancellationTokenSource evacTimerTokenSource = new();
    private CancellationToken evacTimerToken;

    [Dependency] private AudioSystem _audioSys = default!;
    [Dependency] private ChatSystem _chatSys = default!;
    [Dependency] private ThrusterSystem _thrustSys = default!;
    [Dependency] private DockingSystem _dockSys = default!;
    [Dependency] private SharedPhysicsSystem _physSys = default!;
    [Dependency] private RoundEndSystem _roundEndSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImpostorEscapeConditionComponent, ObjectiveGetProgressEvent>(OnObjectiveProgressRequest);
        SubscribeLocalEvent<MindContainerComponent, MobStateChangedEvent>(OnPlayerDeath);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        evacTimerToken = evacTimerTokenSource.Token;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        RuleSelected = EvacActive = EvacFinished = false;
        TriggerDeathCount = CurrentDeathCount = 0;
    }

    private void OnPlayerDeath(EntityUid uid, MindContainerComponent component, MobStateChangedEvent args)
    {
        if (!RuleSelected)
            return;
        
        if (args.NewMobState != MobState.Dead)
            return;

        if (component.HasMind)
        {
            CurrentDeathCount++;
            _audioSys.PlayGlobal("/Audio/Theta/Impostor/flatline.ogg", Filter.Broadcast(), true);
            
            if (HasComp<ImpostorRoleComponent>(component.Mind))
            {
                if (EvacActive && !evacTimerToken.IsCancellationRequested)
                {
                    Announce(Loc.GetString("impostor-announcement-cancelevac"), "/Audio/Announcements/announce.ogg");
                    evacTimerTokenSource.Cancel();
                }
            }
        }

        if (!(EvacActive || EvacFinished) && CurrentDeathCount >= TriggerDeathCount)
        {
            Announce(Loc.GetString("impostor-announcement-beginevac"), "/Audio/Misc/delta.ogg");
            EvacActive = true;
            Timer.Spawn((int)TimeSpan.FromMinutes(LaunchDelay).TotalMilliseconds, LaunchPods, evacTimerToken);
        }
    }

    private void LaunchPods()
    {
        if (!RuleSelected)
            return;
        
        Announce(Loc.GetString("impostor-announcement-endevac"));
        EvacActive = false;
        EvacFinished = true;
        
        foreach ((TransformComponent form, ImpostorLandmarkComponent marker) in EntityQuery<TransformComponent, ImpostorLandmarkComponent>())
        {
            if (marker.Type == ImpostorLandmarkType.EvacPod)
            {
                if (!TryComp(form.GridUid, out ShuttleComponent? shuttle))
                    return;
                
                EntityQueryEnumerator<DockingComponent> dockQuery = EntityQueryEnumerator<DockingComponent>();
                while(dockQuery.MoveNext(out EntityUid uid, out DockingComponent? dock))
                {
                    if (Transform(uid).GridUid != form.GridUid)
                        continue;
                    _dockSys.Undock(uid, dock);
                }
                
                //assuming all the thrusters are located on the same side of the pod
                int index = 0;
                int thrust = 0;
                foreach (double linThrust in shuttle.LinearThrust)
                {
                    if (linThrust > 0)
                    {
                        thrust = (int)linThrust;
                        break;
                    }
                    index++;
                }
                DirectionFlag dir = (DirectionFlag)(int)Math.Pow(2, index); //converting shuttle thrust index to direction
                _thrustSys.EnableLinearThrustDirection(shuttle, dir); //enabling thrusters (purely cosmetic)
                _physSys.ApplyLinearImpulse(form.GridUid.Value, DirectionExtensions.AsDir(dir).ToVec()*thrust*100); //give it a good kick
            }
        }
    }

    private void Announce(string message, string? sound = null)
    {
        _chatSys.DispatchGlobalAnnouncement(message, Loc.GetString("impostor-announcement-sender"), sound != null, 
            sound != null ? new SoundPathSpecifier(sound) : null, Color.DarkRed);
    }

    private void OnObjectiveProgressRequest(EntityUid uid, ImpostorEscapeConditionComponent component, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0;
        
        if (args.Mind.OwnedEntity == null || args.Mind.TimeOfDeath != null)
            return;

        foreach ((TransformComponent form, ImpostorLandmarkComponent marker) in EntityQuery<TransformComponent, ImpostorLandmarkComponent>())
        {
            if (marker.Type == ImpostorLandmarkType.EvacPod)
            {
                if ((Transform(args.Mind.OwnedEntity.Value).LocalPosition - form.LocalPosition).Length() <= 1 && EvacFinished)
                    args.Progress = 1;
            }
        }
    }
}
