using Content.Client.IoC;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Theta.ShipEvent.Console;

public sealed class CannonAmmoStatus : Control
{
    private readonly BoxContainer _bulletsList;
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
            Children =
            {
                new Control() { MinSize = (5, 0) },
                new Control
                {
                    HorizontalExpand = true,
                    Children =
                    {
                        (_bulletsList = new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Horizontal,
                            VerticalAlignment = VAlignment.Center,
                            SeparationOverride = 0
                        }),
                        (_noMagazineLabel = new Label
                        {
                            Text = "No Magazine!",
                            StyleClasses = { StyleNano.StyleClassItemStatus }
                        })
                    }
                },
                new Control() { MinSize = (5, 0) },
                (_ammoCount = new Label
                {
                    StyleClasses = { StyleNano.StyleClassItemStatus },
                    HorizontalAlignment = HAlignment.Right,
                }),
            }
        });
    }

    public void Update(bool magazine, int count, int capacity)
    {
        _bulletsList.RemoveAllChildren();

        if (!magazine)
        {
            _noMagazineLabel.Visible = true;
            _ammoCount.Visible = false;
            return;
        }

        _noMagazineLabel.Visible = false;
        _ammoCount.Visible = true;

        var texturePath = "/Textures/Interface/ItemStatus/Bullets/normal.png";
        var texture = StaticIoC.ResC.GetTexture(texturePath);

        _ammoCount.Text = $"{count}/{capacity}";
        capacity = Math.Min(capacity, 30);
        FillBulletRow(_bulletsList, count, capacity, texture);
    }

    private static void FillBulletRow(Control container, int count, int capacity, Texture texture)
    {
        var colorA = Color.FromHex("#b68f0e");
        var colorB = Color.FromHex("#d7df60");
        var colorGoneA = Color.FromHex("#000000");
        var colorGoneB = Color.FromHex("#222222");

        var altColor = false;

        // Draw the empty ones
        for (var i = count; i < capacity; i++)
        {
            container.AddChild(new TextureRect
            {
                Texture = texture,
                ModulateSelfOverride = altColor ? colorGoneA : colorGoneB,
                Stretch = TextureRect.StretchMode.KeepCentered
            });

            altColor ^= true;
        }

        // Draw the full ones, but limit the count to the capacity
        count = Math.Min(count, capacity);
        for (var i = 0; i < count; i++)
        {
            container.AddChild(new TextureRect
            {
                Texture = texture,
                ModulateSelfOverride = altColor ? colorA : colorB,
                Stretch = TextureRect.StretchMode.KeepCentered
            });

            altColor ^= true;
        }
    }
}
