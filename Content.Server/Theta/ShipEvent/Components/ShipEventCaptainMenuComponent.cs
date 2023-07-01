using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed class ShipEventCaptainMenuComponent : Component
{
    [DataField("toggle", required: true)]
    public InstantAction ToggleAction = new InstantAction();
}
