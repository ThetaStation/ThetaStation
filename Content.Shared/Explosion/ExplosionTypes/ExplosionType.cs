using Content.Shared.Damage;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Explosion.ExplosionTypes;
/// <summary>
/// Class determining 'type' of explosion (how it should behave with different tiles & objects)
/// </summary>
public class ExplosionType
{
    public virtual void ProcessEntity(
        EntityUid entity,
        MapCoordinates epicenter,
        DamageSpecifier? damage,
        float intensity,
        float throwForce,
        string explosionPrototypeId,
        EntityQuery<DamageableComponent> damageQuery,
        EntityQuery<PhysicsComponent> physicsQuery,
        TransformComponent? transform)
    {
    }
}
