using Content.Client.Message;
using Robust.Client.UserInterface.Controls;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Client.UserInterface;

namespace Content.Client.Theta.ShipEvent.UI;

public sealed class TeamListControl : ScrollContainer
{
    public Action<TeamInterfaceState>? TeamSelected;

    //todo: maybe it's better to use grid container after all
    public int FieldCount;

    private BoxContainer _mainContainer = new()
    {
        Orientation = BoxContainer.LayoutOrientation.Vertical,
    };

    public TeamListControl()
    {
        HScrollEnabled = false;
        VerticalExpand = true;
        AddChild(_mainContainer);
    }

    public void Update(List<TeamInterfaceState> states, string? buttonText)
    {
        _mainContainer.DisposeAllChildren();
        FieldCount = 5 + (buttonText != null ? 1 : 0);

        if (states.Count == 0)
        {
            _mainContainer.AddChild(new Label
            {
                Text = Loc.GetString("shipevent-teamlist-empty"),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            });

            return;
        }

        _mainContainer.AddChild(CreateEntry([
            Loc.GetString("shipevent-teamlist-name"),
            Loc.GetString("shipevent-teamlist-captain"),
            Loc.GetString("shipevent-teamlist-crew"),
            Loc.GetString("shipevent-teamlist-points"),
            Loc.GetString("shipevent-teamlist-pass")],
            null,
            null
        ));

        BoxContainer independentContainer = new BoxContainer
        {
            Margin = new(2),
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };
        _mainContainer.AddChild(independentContainer);

        Dictionary<string, BoxContainer> fleetContainers = new();

        foreach (TeamInterfaceState state in states)
        {
            BoxContainer container = independentContainer;
            if (state.Fleet != null)
            {
                if (!fleetContainers.ContainsKey(state.Fleet))
                {
                    BoxContainer newFleetCont = new()
                    {
                        Margin = new(2),
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                    };

                    RichTextLabel label = new();
                    label.SetMarkup($"[bold]{state.Fleet}[/bold]");
                    label.HorizontalAlignment = HAlignment.Center;
                    newFleetCont.AddChild(label);

                    fleetContainers[state.Fleet] = newFleetCont;
                    _mainContainer.AddChild(newFleetCont);
                }

                container = fleetContainers[state.Fleet];
            }

            container.AddChild(CreateEntry(
                [$"[color={state.Color.ToHex()}]{state.Name}[/color]",
                state.Captain ?? "NONE",
                state.Members.ToString() + "/" + (state.MaxMembers == 0 ? "∞" : state.MaxMembers.ToString()),
                state.Points.ToString(),
                state.HasPassword ? "Yes" : "No"],
                buttonText,
                state
            ));
        }
    }

    private BoxContainer CreateEntry(string[] data, string? buttonText, TeamInterfaceState? buttonData)
    {
        BoxContainer container = new();
        container.Orientation = BoxContainer.LayoutOrientation.Horizontal;
        container.HorizontalExpand = true;

        for (int i = 0; i < data.Length; i++)
        {
            RichTextLabel label = new();
            label.SetMarkup(data[i]);
            container.AddChild(label);
        }

        if (buttonText != null && buttonData != null)
        {
            Button button = new();
            button.Text = buttonText;
            button.OnPressed += _ => TeamSelected?.Invoke(buttonData);
            container.AddChild(button);
        }

        container.OnResized += () => ResizeEntry(container);

        return container;
    }

    private void ResizeEntry(BoxContainer container)
    {
        float width = container.Width / FieldCount;
        foreach (Control child in container.Children)
        {
            child.MinWidth = child.MaxWidth = width;
        }
    }
}
