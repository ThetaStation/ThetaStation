using Content.Shared.Roles.Theta;
using Robust.Shared.GameStates;

namespace Content.Shared.Theta.ShipEvent.Components;

/// <summary>
/// Used to mark ships/crewmembers/etc. to avoid wasting time on finding out which team owns this object/get events for <see cref="ShipEventTeamSystem"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShipEventTeamMarkerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public ShipEventTeam? Team;
}
