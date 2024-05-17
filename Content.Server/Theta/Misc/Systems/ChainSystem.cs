using Robust.Shared.Console;
using Content.Server.Administration;
using Content.Server.Theta.Misc.Components;
using Robust.Shared.Physics.Systems;
using Content.Shared.Administration;
using Robust.Server.GameStates;
using Robust.Shared.Physics.Components;
using Content.Shared.Physics;

namespace Content.Server.Theta.Misc.Systems;

public sealed class ChainSystem : EntitySystem
{
    [Dependency] private readonly SharedJointSystem _jointSys = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChainComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<ChainComponent, ComponentRemove>(OnRemoval);
    }

    private void OnInit(EntityUid uid, ChainComponent chain, MapInitEvent args)
    {
        if (chain.BoundUid == null)
            return;

        CreateJoint(uid, chain.BoundUid.Value, chain);
        EnsureComp<ChainComponent>(chain.BoundUid.Value).BoundUid = uid;
    }

    private void OnRemoval(EntityUid uid, ChainComponent chain, ComponentRemove args)
    {
        if (chain.BoundUid == null)
            return;

        chain.BoundUid = null;

        var form = Transform(uid);
        if (form.GridUid == null)
            return;
        _jointSys.RecursiveClearJoints(form.GridUid.Value);
    }

    private void CreateJoint(EntityUid uid, EntityUid otherUid, ChainComponent chain)
    {
        var form1 = Transform(uid);
        var form2 = Transform(otherUid);
        var gridUid1 = form1.GridUid;
        var gridUid2 = form2.GridUid;
        if (gridUid1 == null || gridUid2 == null)
            return;

        var gridPhys1 = Comp<PhysicsComponent>(gridUid1.Value);
        var gridPhys2 = Comp<PhysicsComponent>(gridUid2.Value);

        var joint = _jointSys.CreateDistanceJoint(gridUid1.Value, gridUid2.Value, form1.LocalPosition, form2.LocalPosition);
        SharedJointSystem.AngularStiffness(chain.Frequency, chain.Damping, gridPhys1, gridPhys2, out float stiffness, out float damping);
        joint.Stiffness = stiffness;
        joint.Damping = damping;

        if (TryComp<JointVisualsComponent>(uid, out var visuals))
        {
            visuals.Target = otherUid;
            Dirty(uid, visuals);
        }

        _pvsSys.AddGlobalOverride(uid);
        _pvsSys.AddGlobalOverride(otherUid);
    }
}

[AdminCommand(AdminFlags.Mapping)]
public sealed class CreateChainCommand : IConsoleCommand
{
    public string Command => "create_chain";
    public string Description => "See ChainSystem";
    public string Help => "First arg is chain source uid (must have chain component), second is target uid";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var uid = new EntityUid(int.Parse(args[0]));
        var otherUid = new EntityUid(int.Parse(args[1]));
        entMan.GetComponent<ChainComponent>(uid).BoundUid = otherUid;
    }
}