using Content.Shared.Theta.ShipEvent;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Theta.ShipEvent.UI;

/// <summary>
/// Window for ship event team-view action
/// </summary>
public sealed class TeamViewWindow : DefaultWindow
{
    public TeamViewWindow() { RobustXamlLoader.Load(this); }

    public void UpdateText(TeamViewBoundUserInterfaceState state)
    {
        Robust.Client.UserInterface.Controls.Label teamViewText = this.FindControl<global::Robust.Client.UserInterface.Controls.Label>("TeamViewText");
        teamViewText.Text = state.Text;
    }
}
