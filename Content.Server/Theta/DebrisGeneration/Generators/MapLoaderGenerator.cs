using Robust.Server.Maps;

namespace Content.Server.Theta.DebrisGeneration.Generators;

/// <summary>
/// Generator which simply loads new map from path
/// </summary>
public sealed class MapLoaderGenerator : Generator
{
    [DataField("mapPath", required: true)] 
    public string MapPath = "";
    
    public override EntityUid Generate(DebrisGenerationSystem sys, Vector2 position)
    {
        var loadOptions = new MapLoadOptions
        {
            Rotation = sys.Rand.NextAngle(),
            Offset = position,
            LoadMap = false
        };

        if (sys.MapLoader.TryLoad(sys.TargetMap, MapPath, out var rootUids, loadOptions))
            return rootUids[0];
        
        return EntityUid.Invalid;
    }
}
