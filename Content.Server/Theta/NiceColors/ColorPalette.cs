using Serilog;

namespace Content.Server.Theta.NiceColors;

public abstract class ColorPalette
{
    public abstract List<Color> Palette { get; }

    private int _currentColorPos;

    protected ColorPalette()
    {
        IoCManager.InjectDependencies(this);
    }

    public Color GetNextColor()
    {
        var color = Palette[_currentColorPos];
        _currentColorPos += 1;
        if (_currentColorPos == Palette.Count)
            _currentColorPos = 0;
        return color;
    }

    //for debugging, for example thru https://lawlesscreation.github.io/hex-color-visualiser/
    public void ColorsToConsole()
    {
        string result = "";

        foreach (Color color in Palette)
        {
            result += color.ToHexNoAlpha() + "\n";
        }

        Logger.Info(result);
    }
}
