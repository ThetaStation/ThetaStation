using Content.Server.Chat.Managers;
using Content.Server.Roles;

namespace Content.Server.ShipEvent;

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
        if (Mind.TryGetSession(out var session))
        {
            var chatMgr = IoCManager.Resolve<IChatManager>();
            chatMgr.DispatchServerMessage(session, Loc.GetString("shipevent-role-greet"));
        }
    }
}
