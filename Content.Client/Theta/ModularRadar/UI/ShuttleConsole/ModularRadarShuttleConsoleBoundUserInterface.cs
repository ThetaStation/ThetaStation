using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Events;
using Content.Shared.Theta.ShipEvent;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Client.Theta.ModularRadar.UI.ShuttleConsole;

//todo (radars): separate stealth controls from radar
[UsedImplicitly]
public sealed class ModularRadarShuttleConsoleBoundUserInterface : BoundUserInterface
{
    private ModularRadarShuttleConsoleWindow? _window;

    public ModularRadarShuttleConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = new ModularRadarShuttleConsoleWindow();

        _window.RequestFTL += OnFTLRequest;
        _window.RequestBeaconFTL += OnFTLBeaconRequest;
        _window.DockRequest += OnDockRequest;
        _window.UndockRequest += OnUndockRequest;
        _window.ChangeNamePressed += OnChangeNamePressed;
        _window.StealthButtonPressed += OnStealthButtonPressed;
        _window.OpenCentered();
        SendMessage(new ShipEventRequestStealthStatusMessage());
        _window.OnClose += OnClose;
    }

    private void OnUndockRequest(NetEntity entity)
    {
        SendMessage(new UndockRequestMessage()
        {
            DockEntity = entity,
        });
    }

    private void OnChangeNamePressed(string name)
    {
        SendMessage(new ShuttleConsoleChangeShipNameMessage(name));
    }

    private void OnDockRequest(NetEntity entity, NetEntity target)
    {
        SendMessage(new DockRequestMessage()
        {
            DockEntity = entity,
            TargetDockEntity = target,
        });
    }

    private void OnFTLBeaconRequest(NetEntity ent, Angle angle)
    {
        SendMessage(new ShuttleConsoleFTLBeaconMessage()
        {
            Beacon = ent,
            Angle = angle,
        });
    }

    private void OnFTLRequest(MapCoordinates obj, Angle angle)
    {
        SendMessage(new ShuttleConsoleFTLPositionMessage()
        {
            Coordinates = obj,
            Angle = angle,
        });
    }

    private void OnStealthButtonPressed()
    {
        SendMessage(new ShipEventToggleStealthMessage());
    }

    private void OnClose()
    {
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not ThetaShuttleConsoleBoundInterfaceState cState)
            return;

        _window?.UpdateState(Owner, cState);
    }

    public void SetStealthStatus(bool ready)
    {
        _window?.SetStealthStatus(ready);
    }
}
