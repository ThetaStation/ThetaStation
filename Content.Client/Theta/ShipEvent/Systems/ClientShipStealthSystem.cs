using Content.Client.Theta.ModularRadar.UI.ShuttleConsole;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.Systems;

//todo: read comment in ShipEventTeamSystem.Stealth.cs ((this thing sucks ass))
public sealed class ClientShipStealthSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _uiSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ShipEventStealthStatusMessage>(OnStealthStatusReceived);
    }

    private void OnStealthStatusReceived(ShipEventStealthStatusMessage msg)
    {
        var uid = EntityManager.GetEntity(msg.ConsoleUid);
        if (TryComp<UserInterfaceComponent>(uid, out var ui))
        {
            if (ui.OpenInterfaces.TryGetValue(ShuttleConsoleUiKey.Key, out var bui) && 
            bui is ModularRadarShuttleConsoleBoundUserInterface shuttleConsoleBui)
                shuttleConsoleBui.SetStealthStatus(msg.StealthReady);
        }
    }
}
