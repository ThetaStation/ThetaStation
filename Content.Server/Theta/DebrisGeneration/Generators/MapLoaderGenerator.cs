using System.Numerics;
using Robust.Server.Maps;
using Robust.Shared.Map;

namespace Content.Server.Theta.DebrisGeneration.Generators;

/// <summary>
/// Generator which simply loads new map from path
/// </summary>
public sealed class MapLoaderGenerator : Generator
{
    [DataField("mapPath", required: true)]
    public string MapPath = "";

    public override EntityUid Generate(DebrisGenerationSystem sys, MapId targetMap)
    {
        var loadOptions = new MapLoadOptions
        {
            Rotation = sys.Rand.NextAngle(),
            Offset = Vector2.Zero,
            LoadMap = false
        };

        if (sys.MapLoader.TryLoad(targetMap, MapPath, out var rootUids, loadOptions))
            return rootUids[0];

        return EntityUid.Invalid;
    }
}
