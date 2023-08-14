using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedRadarConsoleSystem))]
public sealed class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float RangeVV
    {
        get => MaxRange;
        set => IoCManager
            .Resolve<IEntitySystemManager>()
            .GetEntitySystem<SharedRadarConsoleSystem>()
            .SetRange(Owner, value, this);
    }

    [DataField("maxRange")]
    public float MaxRange = 256f;

    [DataField("trackedGroups")]
    public readonly Enum TrackedGroups = RadarRenderableGroup.All;
}
