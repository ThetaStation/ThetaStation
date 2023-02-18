using Content.Server.Theta.ShipEvent.Systems;

namespace Content.Server.Theta.ShipEvent.Components;

/// <summary>
/// Used to mark ships/crewmembers/etc. to avoid wasting time on finding out which team owns this object/get events for <see cref="ShipEventFactionSystem"/>
/// </summary>
[RegisterComponent]
public sealed class ShipEventFactionMarkerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public ShipEventFaction? Team;
}
