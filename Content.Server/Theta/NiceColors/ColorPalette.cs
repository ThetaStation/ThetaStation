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
        Log.Debug(_currentColorPos.ToString());
        return color;
    }
}
