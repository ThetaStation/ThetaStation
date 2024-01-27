using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem
{
    public float AnomalySpawnInterval;
    public List<EntityPrototype> AnomalyPrototypes = new();

    private void AnomalyUpdate()
    {
        if (AnomalyPrototypes.Count == 0)
            return;

        string protId = ListPick(AnomalyPrototypes).ID;
        for (int c = 0; c < 50; c++)
        {
            Box2 bounds = GetPlayAreaBounds();
            Vector2 pos = _random.NextVector2Box(bounds.BottomLeft.X, bounds.BottomLeft.Y, bounds.TopRight.X, bounds.TopRight.Y);

            if (_mapMan.TryFindGridAt(new(pos, TargetMap), out _, out _))
                continue;

            SpawnAtPosition(protId, new(_mapMan.GetMapEntityId(TargetMap), pos));
            break;
        }
    }
}