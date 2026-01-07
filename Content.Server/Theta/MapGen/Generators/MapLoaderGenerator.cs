using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Theta.MapGen.Generators;

public sealed partial class MapLoaderGenerator : IMapGenGenerator
{
    [DataField(required: true)]
    public ResPath MapPath;

    public IEnumerable<EntityUid> Generate(MapGenSystem sys, MapId targetMap)
    {
        if (sys.MapLoader.TryLoadMap(MapPath, out _, out var gridUids))
            return (IEnumerable<EntityUid>)gridUids;

        return [];
    }
}
