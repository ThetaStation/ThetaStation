using Content.Shared.Interaction;
using Robust.Shared.Serialization;
using Serilog;

namespace Content.Shared.Theta.ShipEvent;

public sealed class SharedCannonSystem : EntitySystem
{
    [Dependency] private RotateToFaceSystem _rotateToFaceSystem = default!;

    private Dictionary<EntityUid, Vector2> _toUpdateRotation = new();

    public override void Initialize()
    {
        SubscribeAllEvent<RotateCannonEvent>(RotateCannons);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (uid, coordinates) in _toUpdateRotation)
        {
            _rotateToFaceSystem.TryFaceCoordinates(uid, coordinates);
        }
        _toUpdateRotation.Clear();
    }

    private void RotateCannons(RotateCannonEvent args)
    {
        foreach (var uid in args.Cannons)
        {
            _toUpdateRotation[uid] = args.Coordinates;
        }
    }
}

[Serializable, NetSerializable]
public sealed class RotateCannonEvent : EntityEventArgs
{
    public readonly Vector2 Coordinates;

    public readonly List<EntityUid> Cannons;

    public RotateCannonEvent(Vector2 coordinates, List<EntityUid> cannons)
    {
        Coordinates = coordinates;
        Cannons = cannons;
    }
}
