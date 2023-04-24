using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Radar.Widgets;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Systems.Radar;

public sealed class RadarUIController : UIController
{
    private RadarGui? RadarGui => UIManager.GetActiveUIWidgetOrNull<RadarGui>();

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        RadarGui!.Visible = true;
    }

    private void OnScreenUnload()
    {
        RadarGui!.Visible = false;
    }
}
