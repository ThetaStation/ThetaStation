using Content.Server.Theta.Impostor.Components;
using Content.Shared.GameTicking;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.Theta.Impostor.Systems;

public sealed class ImpostorBombObjectiveSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaSys = default!;
    [Dependency] private IRobustRandom _rand = default!;
    
    private HashSet<EntityUid> BlownUpLandmarks = new();
    
    public override void Initialize()
    {
        SubscribeLocalEvent<ImpostorBombConditionComponent, ObjectiveGetProgressEvent>(OnObjectiveProgressRequest);
        SubscribeLocalEvent<ImpostorBombConditionComponent, ObjectiveAfterAssignEvent>(OnObjectiveAssign);
        SubscribeLocalEvent<ImpostorLandmarkComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        BlownUpLandmarks.Clear();
    }
    
    private void OnParentChanged(EntityUid uid, ImpostorLandmarkComponent component, ref EntParentChangedMessage args)
    {
        if (Transform(uid).GridUid == null)
        {
            BlownUpLandmarks.Add(uid);
            Del(uid);
        }
    }
    
    private void OnObjectiveAssign(EntityUid uid, ImpostorBombConditionComponent component, ref ObjectiveAfterAssignEvent args)
    {
        List<EntityUid> bombMarks = new();
        while (EntityQueryEnumerator<ImpostorLandmarkComponent>().MoveNext(out EntityUid markUid, out ImpostorLandmarkComponent? mark))
        {
            if(mark.Type == ImpostorLandmarkType.ImpostorBombLocation)
                bombMarks.Add(markUid);
        }
        component.TargetLandmark = _rand.Pick(bombMarks);

        _metaSys.SetEntityDescription(uid, Loc.GetString("impostor-objectives-bombdesc", 
            ("name", Comp<MetaDataComponent>(component.TargetLandmark.Value).EntityName)));
    }
    
    private void OnObjectiveProgressRequest(EntityUid uid, ImpostorBombConditionComponent component, ref ObjectiveGetProgressEvent args)
    {
        if (component.TargetLandmark == null)
        {
            Log.Error($"Tried to request progress for bomb objective without target landmark. Objective holder: {args.Mind.OwnedEntity}.");
            args.Progress = 0;
            return;
        }

        args.Progress = BlownUpLandmarks.Contains(component.TargetLandmark.Value) ? 1 : 0;
    }
}
