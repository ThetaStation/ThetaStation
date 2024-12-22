using Robust.Shared.Random;

namespace Content.Server.Theta.NiceColors.ColorPalettes;

public sealed class ShipEventPalette : ColorPalette
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private const double MinLuminosity = 120;
    private const double MaxLuminosity = 180;
    private const double MinAverageDelta = 10;

    private List<Color> _palette;
    public override List<Color> Palette => _palette;

    public ShipEventPalette()
    {
        _palette = GetPalette();
        _random.Shuffle(_palette);
    }

    private List<Color> GetPalette()
    {
        var colors = new List<Color>();

        foreach (var (_, color) in Color.GetAllDefaultColors())
        {
            if (color.AByte != 0xFF)
                continue;

            //filtering out greyish colors
            double rgbAvg = (color.RByte + color.GByte + color.BByte) / 3;
            if (Math.Abs(color.RByte - rgbAvg) < MinAverageDelta)
                continue;

            //https://stackoverflow.com/questions/26233781/detect-the-brightness-of-a-pixel-or-the-area-surrounding-it
            double perceivedLuminosity = color.RByte * 0.2126 + color.GByte * 0.7152 + color.BByte * 0.0722;
            if (perceivedLuminosity < MinLuminosity || perceivedLuminosity > MaxLuminosity)
                continue;

            colors.Add(color);
        }

        return colors;
    }
}
