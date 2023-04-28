using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Radar.Widgets;
using Content.Shared.Theta.RadarHUD;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Radar;

public sealed class RadarUIController : UIController
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private RadarGui? RadarGui => UIManager.GetActiveUIWidgetOrNull<RadarGui>();

    public override void Initialize()
    {
        SubscribeLocalEvent<RadarHudComponentAdded>(OnRadarHudAdded);
        SubscribeLocalEvent<RadarHudComponentRemoved>(OnRadarHudRemoved);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        UpdateGui();
    }

    private void OnScreenUnload()
    {
        UpdateGui();
    }

    private void OnPlayerAttach(PlayerAttachedEvent ev)
    {
        UpdateGui();
    }

    private void UpdateGui()
    {
        if (RadarGui == null)
            return;
        RadarGui.Visible = EntityManager.HasComponent<RadarHUDComponent>(_playerManager.LocalPlayer?.ControlledEntity);
    }

    private void OnRadarHudAdded(ref RadarHudComponentAdded ev)
    {
        if (RadarGui == null)
            return;
        RadarGui.Visible = true;
    }

    private void OnRadarHudRemoved(ref RadarHudComponentRemoved ev)
    {
        if (RadarGui == null)
            return;
        RadarGui.Visible = false;
    }
}
