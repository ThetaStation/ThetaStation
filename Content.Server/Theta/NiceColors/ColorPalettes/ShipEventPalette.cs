using Robust.Shared.Random;

namespace Content.Server.Theta.NiceColors.ColorPalettes;

public sealed class ShipEventPalette : ColorPalette
{
    [Dependency] private readonly IRobustRandom _random = default!;

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
            if(color.AByte != 0xFF)
                continue;
            const double lightLuminosity = 255f / 2f;
            if(color.RByte * 0.2126 + color.GByte * 0.7152 + color.BByte * 0.0722 < lightLuminosity)
                continue;
            colors.Add(color);
        }

        return colors;
    }
}
