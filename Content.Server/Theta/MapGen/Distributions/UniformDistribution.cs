using System.Numerics;

namespace Content.Server.Theta.MapGen.Distributions;

public sealed partial class UniformDistribution : IMapGenDistribution
{
    public Vector2 Generate(MapGenSystem sys)
    {
        return sys.Random.NextVector2Box(sys.Area.Left, sys.Area.Bottom, sys.Area.Right, sys.Area.Top);
    }
}