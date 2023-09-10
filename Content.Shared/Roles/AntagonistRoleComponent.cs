using Content.Shared.Roles.Theta;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Roles;

public abstract partial class AntagonistRoleComponent : Component
{
    [DataField("prototype", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string? PrototypeId;

    /// <summary>
    ///     Faction to which this role is assigned.
    /// </summary>
    [ViewVariables]
    public PlayerFaction? Faction { get; set; }
}
