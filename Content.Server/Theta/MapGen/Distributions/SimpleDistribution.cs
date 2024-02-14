using System.Numerics;

namespace Content.Server.Theta.MapGen.Distributions;

public sealed class SimpleDistribution : Distribution
{
    public override Vector2 Generate(MapGenSystem sys)
    {
        return new Vector2(sys.Random.NextFloat(sys.MaxSpawnOffset), sys.Random.NextFloat(sys.MaxSpawnOffset)) + sys.StartPos;
    }
}