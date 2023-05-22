using Robust.Shared.Serialization;

namespace Content.Shared.Theta;

//REMOVE LATER
[Serializable, NetSerializable]
public sealed class FinalGridStateEvent : EntityEventArgs
{
    public List<Box2i> FreeRangeRects;
    public List<Vector2i> SpawnPositions;

    public FinalGridStateEvent(List<Box2i> freeRangeRects, List<Vector2i> spawnPositions)
    {
        FreeRangeRects = freeRangeRects;
        SpawnPositions = spawnPositions;
    }
}
