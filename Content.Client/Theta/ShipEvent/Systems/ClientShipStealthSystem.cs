using Content.Client.Theta.ModularRadar.UI.ShuttleConsole;
using Content.Shared.Shuttles.Components;
using Content.Shared.Theta.ShipEvent;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.Systems;

//todo: read comment in SHipEventFactionSystem.Stealth.cs ((this thing sucks ass))
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
        var uid = EntityManager.GetEntity(msg.Console);
        if (TryComp<UserInterfaceComponent>(uid, out var ui))
        {
            if (ui.OpenInterfaces[ShuttleConsoleUiKey.Key] is ModularRadarShuttleConsoleBoundUserInterface shuttleConsoleBui)
                shuttleConsoleBui.SetStealthStatus(msg.StealthReady);
        }
    }
}
