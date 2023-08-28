using System.Linq;
using System.Numerics;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Mind.Components;
using Content.Server.Spawners.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Theta.TeamDeathmatch;

[RegisterComponent]
public sealed class TeamDeathmatchMarkerComponent : Component
{
    [DataField("team")]
    public bool Team; //true is red, false is blue
}

[RegisterComponent]
public sealed class TeamDeathmatchObeliskComponent : Component { }

public sealed class TeamDeathmatchSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSys = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvSys = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly MapSystem _mapSys = default!;
    [Dependency] private readonly IPrototypeManager _protMan = default!;
    [Dependency] private readonly MobStateSystem _mobStateSys = default!;

    public bool RuleSelected;

    public int RespawnDelay;
    public int BonusKillsForObelisk;
    public List<string> GunPrototypes = new();

    private int _redKills;
    private int _blueKills;

    private Vector2 redSpawnPosition;
    private Vector2 blueSpawnPosition;

    private const string RedJobId = "PassengerRed";
    private const string BlueJobId = "PassengerBlue";
    private const string MapPoolId = "TeamDeathmatchMapPool";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TeamDeathmatchMarkerComponent, MobStateChangedEvent>(OnPlayerStateChange);
        SubscribeLocalEvent<TeamDeathmatchMarkerComponent, SuicideEvent>(OnSuicideAttempt);
        SubscribeLocalEvent<TeamDeathmatchObeliskComponent, ComponentRemove>(OnObeliskDestroyed);
        //SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnd);
    }

    private void OnSuicideAttempt(EntityUid uid, TeamDeathmatchMarkerComponent marker, SuicideEvent args)
    {
        args.BlockSuicideAttempt(true);
        if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState))
            _mobStateSys.ChangeMobState(uid, MobState.Dead, mobState);
    }

    private void OnObeliskDestroyed(EntityUid uid, TeamDeathmatchObeliskComponent obelisk, ComponentRemove args)
    {
        if (EntityManager.TryGetComponent<TeamDeathmatchMarkerComponent>(uid, out var marker))
        {
            _chatSys.DispatchGlobalAnnouncement(Loc.GetString(marker.Team ? "tdm-obelisk-destroyed-red" : "tdm-obelisk-destroyed-blue", ("bonus", BonusKillsForObelisk)));
            if (marker.Team) _blueKills += BonusKillsForObelisk;
            else _redKills += BonusKillsForObelisk;
        }
    }

    public void UpdateSpawnPositions()
    {
        while (EntityQueryEnumerator<SpawnPointComponent>().MoveNext(out var uid, out var spawn))
        {
            if(spawn.Job == null)
                continue;
            
            if (spawn.Job.ID == RedJobId)
                redSpawnPosition = Transform(uid).LocalPosition;
            
            if (spawn.Job.ID == BlueJobId)
                blueSpawnPosition = Transform(uid).LocalPosition;
        }
    }

    private void OnPlayerStateChange(EntityUid uid, TeamDeathmatchMarkerComponent marker, MobStateChangedEvent args)
    {
        IPlayerSession? session = null;
        if (EntityManager.TryGetComponent<MindContainerComponent>(uid, out var mindComp))
        {
            if (mindComp.HasMind)
                session = mindComp.Mind!.Session;
        }

        if (session == null)
            return;
        
        if (args.NewMobState == MobState.Dead)
        {
            _chatSys.SendSimpleMessage(Loc.GetString("tdm-dead", ("seconds", RespawnDelay)), session, color: marker.Team ? Color.Red : Color.Blue);
            Timer.Spawn(RespawnDelay * 1000, () => Respawn(uid, marker));
        }
    }

    private void Respawn(EntityUid uid, TeamDeathmatchMarkerComponent marker)
    {
        Transform(uid).LocalPosition = marker.Team ? redSpawnPosition : blueSpawnPosition;
        _rejuvSys.PerformRejuvenate(uid);

        if (EntityManager.TryGetComponent<HandsComponent>(uid, out var hands))
        {
            if (hands.ActiveHand?.Container != null && hands.ActiveHand.IsEmpty)
            {
                EntityUid newGun = EntityManager.SpawnEntity(_random.Pick(GunPrototypes), new EntityCoordinates(uid, Vector2.Zero));
                
                //joke - hands.ActiveHand.Container.Insert() is invalid due to access restrictions,
                //but putting this container into variable and calling Insert() again is perfectly fine.
                ContainerSlot handContainer = hands.ActiveHand.Container;
                handContainer.Insert(newGun);
            }
        }
        
        if (marker.Team) _blueKills++;
        else _redKills++;
    }
}
