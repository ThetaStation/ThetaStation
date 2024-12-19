using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Radar.Widgets;
using Content.Shared.Theta.RadarHUD;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;

namespace Content.Client.UserInterface.Systems.Radar;

public sealed class RadarUIController : UIController
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private RadarGui? RadarGui;

    public override void Initialize()
    {
        SubscribeLocalEvent<RadarHudComponentAdded>(OnRadarHudAdded);
        SubscribeLocalEvent<RadarHudComponentRemoved>(OnRadarHudRemoved);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetach);

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        InitializeRadarGui();
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent ev)
    {
        InitializeRadarGui();
    }

    private void OnPlayerDetach(LocalPlayerDetachedEvent ev)
    {
        ClearRadarGui();
    }

    private void OnRadarHudAdded(ref RadarHudComponentAdded ev)
    {
        InitializeRadarGui();
    }

    private void OnRadarHudRemoved(ref RadarHudComponentRemoved ev)
    {
        ClearRadarGui();
    }

    private void OnScreenUnload()
    {
        ClearRadarGui();
    }

    public void UpdateRadarMatrix()
    {
        if (RadarGui == null)
            return;

        if (_playerManager.LocalSession?.AttachedEntity == null)
            return;

        var transform =
            EntityManager.GetComponent<TransformComponent>(_playerManager.LocalSession.AttachedEntity.Value);

        RadarGui.SetMatrix(transform.Coordinates, _eyeManager.CurrentEye.Rotation);
    }

    private void InitializeRadarGui()
    {
        if (_playerManager.LocalSession?.AttachedEntity == null)
            return;

        if (!EntityManager.HasComponent<RadarHUDComponent>(_playerManager.LocalSession.AttachedEntity))
        {
            ClearRadarGui();
            return;
        }

        if (RadarGui != null || UIManager.ActiveScreen == null)
            return;

        RadarGui = new();
        RadarGui.SetOwner(_playerManager.LocalSession.AttachedEntity.Value);
        switch (UIManager.ActiveScreen)
        {
            case DefaultGameScreen game:
                game.Radar.AddChild(RadarGui);
                LayoutContainer.SetAnchorAndMarginPreset(game.Radar, LayoutContainer.LayoutPreset.BottomRight, margin: 10);
                break;
            case SeparatedChatGameScreen separated:
                separated.Radar.AddChild(RadarGui);
                LayoutContainer.SetAnchorAndMarginPreset(separated.Radar, LayoutContainer.LayoutPreset.BottomRight, margin: 10);
                break;
        }
    }

    private void ClearRadarGui()
    {
        if (RadarGui == null)
            return;
        RadarGui.Orphan();
        RadarGui = null;
    }
}
