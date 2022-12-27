using Content.Shared.Actions.ActionTypes;

namespace Content.Server.ShipEvent.Components;

[RegisterComponent]
public sealed class ShipEventFactionViewComponent : Component
{
    [DataField("toggle", required: true)]
    public InstantAction ToggleAction = new InstantAction();
}
