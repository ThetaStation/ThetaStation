using System.Numerics;
using Robust.Shared.Noise;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Theta.MapGen.Distributions;

public sealed class NoiseDistribution : Distribution
{
    private List<Vector2> _positions = new();
    private int _lastIndex;

    //total amount of positions = resolution ^ 2
    public NoiseDistribution(FastNoiseLite.NoiseType noiseType, int resolution, float frequency, float threshold)
    {
        var generator = new FastNoiseLite();
        generator.SetNoiseType(noiseType);
        generator.SetFrequency(frequency);
        FillPositions(generator, resolution, threshold);
    }

    public NoiseDistribution(FastNoiseLite generator, int resolution, float threshold)
    {
        FillPositions(generator, resolution, threshold);
    }

    private void FillPositions(FastNoiseLite generator, int resolution, float threshold)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normalX = (float) x / resolution;
                float normalY = (float) y / resolution;
                float value = generator.GetNoise(normalX, normalY);
                if (value > threshold)
                    _positions.Add(new Vector2(normalX, normalY));
            }
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