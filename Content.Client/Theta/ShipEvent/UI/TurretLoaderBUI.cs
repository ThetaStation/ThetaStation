using Content.Shared.Theta.ShipEvent.Components;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.UI;

public sealed class TurretLoaderBoundUserInterface : BoundUserInterface
{
    private IEntityManager _entMan = default!;
    private TurretLoaderWindow? _window;

    public TurretLoaderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _entMan = IoCManager.Resolve<IEntityManager>();

        _window = new TurretLoaderWindow();
        _window.OpenCentered();

        _window.OnClose += Close;
        _window.EjectButtonPressed += _ => { SendMessage(new TurretLoaderEjectRequest()); };
        _window.RefreshButtonPressed += _ => { _window.UpdateState(Refresh()); };

        _window.UpdateState(Refresh());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not TurretLoaderBoundUserInterfaceState loaderState)
            return;

        base.UpdateState(state);
        _window?.UpdateState(loaderState);
    }

    private TurretLoaderBoundUserInterfaceState Refresh()
    {
        if (_entMan.TryGetComponent<TurretLoaderComponent>(Owner, out var loader))
        {
            var ammoCount = 0;

            //slot contents check may seem redundant, when AmmoContainer is updated based on slot contents anyway when loader component
            //changes state, however due to unknown reasons AmmoContainer is sometimes updated incorrectly, so it's better to double-check
            if (loader.AmmoContainer != null && loader.ContainerSlot?.Item != null)
                ammoCount = loader.AmmoContainer.ContainedEntities.Count;

            return new TurretLoaderBoundUserInterfaceState(
                ammoCount,
                loader.BoundTurret.GetHashCode(),
                GetLoaderStatus(loader));
        }

        return new TurretLoaderBoundUserInterfaceState(0,0,"-");
    }

    private string GetLoaderStatus(TurretLoaderComponent loader)
    {
        if (!_entMan.EntityExists(loader.BoundTurret))
            return Loc.GetString("shipevent-turretloader-status-unbound");

        if (loader.ContainerSlot?.Item == null)
            return Loc.GetString("shipevent-turretloader-status-nocontainer");

        return Loc.GetString("shipevent-turretloader-status-normal");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
