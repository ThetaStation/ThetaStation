using Content.Client.Stylesheets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Theta.ShipEvent.Console;

public sealed class CannonAmmoStatus : Control
{
    private readonly ProgressBar _ammoBar;
    private readonly Label _noMagazineLabel;
    private readonly Label _ammoCount;

    public CannonAmmoStatus()
    {
        MinHeight = 15;
        HorizontalExpand = true;
        VerticalAlignment = VAlignment.Center;

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(0, 1),
            Children =
            {
                new Control { MinSize = (5, 0) },
                new Control
                {
                    HorizontalExpand = true,
                    Children =
                    {
                        (_ammoBar = new ProgressBar
                        {
                            MinValue = 0,
                        }),
                        (_noMagazineLabel = new Label
                        {
                            Text = "No Magazine!",
                            StyleClasses = { StyleNano.StyleClassItemStatus },
                            Align = Label.AlignMode.Center
                        }),
                        (_ammoCount = new Label
                        {
                            StyleClasses = { StyleNano.StyleClassItemStatus },
                            Align = Label.AlignMode.Center
                        })
                    }
                },
                new Control { MinSize = (5, 0) },
            }
        });
    }

    //usedCapacity is how much of storage space of loader's container is occupied (for drawing ammo bar correctly)
    public void Update(bool magazine, int count, int usedCapacity, int maxCapacity)
    {
        if (!magazine)
        {
            _noMagazineLabel.Visible = true;
            _ammoCount.Visible = false;
            _ammoBar.Value = 0;
            return;
        }

        _noMagazineLabel.Visible = false;

        _ammoCount.Text = $"{count}";

        _ammoBar.MaxValue = maxCapacity;
        _ammoBar.Value = usedCapacity;
    }
}
