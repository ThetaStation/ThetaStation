using System.Numerics;
using Robust.Shared.Noise;
using Robust.Shared.Random;

namespace Content.Server.Theta.MapGen.Distributions;

public sealed partial class NoiseDistribution : IMapGenDistribution
{
    [DataField] public FastNoiseLite.NoiseType NoiseType;
    [DataField] public float Frequency;
    [DataField] public float Threshold;

    private List<Vector2> _positions = new();
    private int _lastIndex;

    private void FillPositions(FastNoiseLite generator, Box2i area, int sectorSize, float threshold)
    {
        int sectorsX = area.Width / sectorSize;
        int sectorsY = area.Height / sectorSize;

        var random = IoCManager.Resolve<IRobustRandom>();
        for (int y = 0; y < sectorsX; y++)
        {
            for (int x = 0; x < sectorsY; x++)
            {
                float normalX = (float) x / sectorsX;
                float normalY = (float) y / sectorsY;
                float value = generator.GetNoise(normalX, normalY);

                if (value > threshold)
                    _positions.Add(new Vector2(normalX * area.Width, normalY * area.Height));
            }
        }
    }

    public Vector2 Generate(MapGenSystem sys)
    {
        if (_positions.Count == 0)
        {
            var generator = new FastNoiseLite();
            generator.SetNoiseType(NoiseType);
            generator.SetFrequency(Frequency);
            FillPositions(generator, sys.Area, MapGenSystem.SectorSize, Threshold);
        }

        Vector2 pos = _positions[_lastIndex];
        _lastIndex++;
        if (_lastIndex > _positions.Count - 1)
            _lastIndex = 0;
        return pos;
    }
}