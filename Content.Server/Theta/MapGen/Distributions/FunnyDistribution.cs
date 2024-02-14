using System.Numerics;

namespace Content.Server.Theta.MapGen.Distributions;

public sealed class FunnyDistribution : Distribution
{
    private List<Vector2> _positions = new();
    private int _lastIndex;

    public FunnyDistribution()
    {
        string raw = Loc.GetString("theta-distribution");
        string[] coords = raw.Split(',');
        for (int i = 0; i < coords.Length - 2; i += 2)
        {
            _positions.Add(new Vector2(float.Parse(coords[i]), float.Parse(coords[i + 1])));
        }
    }

    public override Vector2 Generate(MapGenSystem sys)
    {
        Vector2 pos = _positions[_lastIndex] * sys.MaxSpawnOffset + sys.StartPos;
        _lastIndex++;
        if (_lastIndex > _positions.Count - 1)
            _lastIndex = 0;
        return pos;
    }
}