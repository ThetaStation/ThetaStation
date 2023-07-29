using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Roles;

namespace Content.Server.Theta.ShipEvent;

public sealed class ShipEventRole : Role
{
    [ViewVariables]
    public override string Name { get; }

    [ViewVariables]
    public override bool Antagonist { get; }

    public ShipEventRole(Mind.Mind mind) : base(mind)
    {
        Name = Loc.GetString("shipevent-role-name");
        Antagonist = true;
    }

    public override void Greet()
    {
        var mindSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<MindSystem>();
        if (mindSystem.TryGetSession(Mind, out var session))
        {
            var chatMgr = IoCManager.Resolve<IChatManager>();
            chatMgr.DispatchServerMessage(session, Loc.GetString("shipevent-role-greet"));
        }
    }
}
