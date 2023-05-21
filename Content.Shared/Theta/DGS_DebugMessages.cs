using Robust.Shared.Serialization;

namespace Content.Shared.Theta;

//REMOVE LATER
[Serializable, NetSerializable]
public sealed class FinalGridStateEvent : EntityEventArgs
{
    public List<Box2i> FreeRangeRects;
    public List<Box2i> OccupiedRangeRects;

    public FinalGridStateEvent(List<Box2i> freeRangeRects, List<Box2i> occupiedRangeRects)
    {
        FreeRangeRects = freeRangeRects;
        OccupiedRangeRects = occupiedRangeRects;
    }
}
