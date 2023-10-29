using Content.Server.Theta.Impostor.Components;
using Content.Shared.GameTicking;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Theta.Impostor.Systems;

public sealed class ImpostorBombObjectiveSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaSys = default!;
    [Dependency] private IRobustRandom _rand = default!;
    
    private HashSet<string> BlownUpLandmarkNames = new();
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ImpostorBombConditionComponent, ObjectiveGetProgressEvent>(OnObjectiveProgressRequest);
        SubscribeLocalEvent<ImpostorBombConditionComponent, ObjectiveAfterAssignEvent>(OnObjectiveAssign);
        SubscribeLocalEvent<ImpostorLandmarkComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        BlownUpLandmarkNames.Clear();
    }
    
    private void OnParentChanged(EntityUid uid, ImpostorLandmarkComponent component, ref EntParentChangedMessage args)
    {
        if (Transform(uid).GridUid == null)
        {
            BlownUpLandmarkNames.Add(Comp<MetaDataComponent>(uid).EntityName);
            QueueDel(uid);
        }
    }
    
    private void OnObjectiveAssign(EntityUid uid, ImpostorBombConditionComponent component, ref ObjectiveAfterAssignEvent args)
    {
        List<EntityUid> bombMarks = new();
        EntityQueryEnumerator<ImpostorLandmarkComponent> query = EntityQueryEnumerator<ImpostorLandmarkComponent>();
        while (query.MoveNext(out EntityUid markUid, out ImpostorLandmarkComponent? mark))
        {
            if(mark.Type == ImpostorLandmarkType.ImpostorBombLocation)
                bombMarks.Add(markUid);
        }
        component.TargetLandmarkName = Comp<MetaDataComponent>(_rand.Pick(bombMarks)).EntityName;

        _metaSys.SetEntityDescription(uid, Loc.GetString("impostor-objectives-bombdesc", 
            ("name", component.TargetLandmarkName)));
    }
    
    private void OnObjectiveProgressRequest(EntityUid uid, ImpostorBombConditionComponent component, ref ObjectiveGetProgressEvent args)
    {
        if (component.TargetLandmarkName == null)
        {
            Log.Error($"Tried to request progress for bomb objective without target landmark. Objective holder: {args.Mind.OwnedEntity}.");
            args.Progress = 0;
            return;
        }

        args.Progress = BlownUpLandmarkNames.Contains(component.TargetLandmarkName) ? 1 : 0;
    }
}
