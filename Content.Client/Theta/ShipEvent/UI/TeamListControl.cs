using Content.Client.Message;
using Robust.Client.UserInterface.Controls;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Client.UserInterface;

namespace Content.Client.Theta.ShipEvent.UI;

public sealed class TeamListControl : BoxContainer
{
    public Action<TeamInterfaceState>? TeamSelected;

    //todo: maybe it's better to use grid container after all
    public int FieldCount;

    public TeamListControl()
    {
        VerticalExpand = true;
        HorizontalExpand = true;
        Orientation = LayoutOrientation.Vertical;
    }

    public void Update(List<TeamInterfaceState> states, string? buttonText)
    {
        DisposeAllChildren();
        FieldCount = 5 + (buttonText != null ? 1 : 0);

        if (states.Count == 0)
        {
            AddChild(new Label
            {
                Text = Loc.GetString("shipevent-teamlist-empty"),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            });

            return;
        }

        AddChild(CreateEntry([
            Loc.GetString("shipevent-teamlist-name"),
            Loc.GetString("shipevent-teamlist-captain"),
            Loc.GetString("shipevent-teamlist-crew"),
            Loc.GetString("shipevent-teamlist-points"),
            Loc.GetString("shipevent-teamlist-pass")],
            null,
            null
        ));

        ScrollContainer independentScroll = new()
        {
            Margin = new(2),
            VerticalExpand = true,
            HScrollEnabled = false
        };
        BoxContainer independentContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            MinHeight = 100,
            MaxHeight = 300
        };

        RichTextLabel independentLabel = new();
        independentLabel.SetMarkup($"[bold]{Loc.GetString("shipevent-teamlist-indies")}[/bold]");
        independentLabel.HorizontalAlignment = HAlignment.Center;
        independentContainer.AddChild(independentLabel);

        independentScroll.AddChild(independentContainer);
        AddChild(independentScroll);

        Dictionary<string, BoxContainer> fleetContainers = new();

        foreach (TeamInterfaceState state in states)
        {
            BoxContainer container = independentContainer;
            if (state.Fleet != null)
            {
                if (!fleetContainers.ContainsKey(state.Fleet))
                {
                    ScrollContainer fleetScroll = new ScrollContainer()
                    {
                        Margin = new(2),
                        VerticalExpand = true,
                        HScrollEnabled = false
                    };
                    BoxContainer fleetContainer = new()
                    {
                        Orientation = LayoutOrientation.Vertical,
                        MinHeight = 100,
                        MaxHeight = 300
                    };

                    RichTextLabel label = new();
                    label.SetMarkup($"[bold]{state.Fleet}[/bold]");
                    label.HorizontalAlignment = HAlignment.Center;
                    fleetContainer.AddChild(label);
                    fleetContainers[state.Fleet] = fleetContainer;

                    fleetScroll.AddChild(fleetContainer);
                    AddChild(fleetScroll);
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

        if (independentContainer.ChildCount == 0)
            RemoveChild(independentScroll);
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
