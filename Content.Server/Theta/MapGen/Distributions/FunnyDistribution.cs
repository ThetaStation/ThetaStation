using System.Globalization;
using System.Numerics;

namespace Content.Server.Theta.MapGen.Distributions;

public sealed partial class FunnyDistribution : IMapGenDistribution
{
    private List<Vector2> _positions = new();
    private int _lastIndex;

    private void FillPositions()
    {
        string raw = Loc.GetString("theta-distribution");
        string[] coords = raw.Split(',');
        for (int i = 0; i < coords.Length - 2; i += 2)
        {
            _positions.Add(new Vector2(float.Parse(coords[i], CultureInfo.InvariantCulture), float.Parse(coords[i + 1], CultureInfo.InvariantCulture)));
        }
    }

    public Vector2 Generate(MapGenSystem sys)
    {
        if (_positions.Count == 0)
            FillPositions();

        Vector2 pos = _positions[_lastIndex] * new Vector2(sys.Area.Width, sys.Area.Height);
        _lastIndex++;
        if (_lastIndex > _positions.Count - 1)
            _lastIndex = 0;
        return pos;
    }
}
