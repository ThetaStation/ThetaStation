using Content.Client.UserInterface.Systems.Radar;
using Content.Shared.Theta.RadarHUD;
using Robust.Client.UserInterface;

namespace Content.Client.Theta.ShipEvent.Systems;

public sealed class RadarHUDSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Update(float frameTime)
    {
        var uiController = _uiManager.GetUIController<RadarUIController>();
        uiController.UpdateRadarMatrix();
    }
}
