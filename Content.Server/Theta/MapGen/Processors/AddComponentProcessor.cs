using Content.Shared.Theta;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Theta.MapGen.Processors;

/// <summary>
/// Processor which adds specified components onto processed grid (not grid entities)
/// </summary>
[UsedImplicitly]
public sealed partial class AddComponentsProcessor : IMapGenProcessor
{
    [DataField("components", required: true)]
    [AlwaysPushInheritance]
    public ComponentRegistry Components = new();

    public void Process(MapGenSystem sys, MapId targetMap, EntityUid gridUid, bool isGlobal)
    {
        if (isGlobal)
        {
            foreach (var childGridUid in sys.SpawnedGrids)
            {
                ThetaHelpers.AddComponentsFromRegistry(gridUid, Components);
            }
        }
        else
        {
            ThetaHelpers.AddComponentsFromRegistry(gridUid, Components);
        }
    }
}
