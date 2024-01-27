using System.Numerics;
using Content.Server.Theta.MapGen.Prototypes;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem
{
    public float AnomalySpawnInterval;
    public List<StructurePrototype> AnomalyPrototypes = new();

    private void AnomalyUpdate()
    {
        if (AnomalyPrototypes.Count == 0)
            return;

        _mapGenSys.RandomPosSpawn(
        TargetMap,
        Vector2.Zero,
        MaxSpawnOffset,
        50,
        ListPick(AnomalyPrototypes));
    }
}