using Content.Shared.Actions.ActionTypes;

namespace Content.Server.Theta.ShipEvent.Components;

public sealed class ShipEventCaptainMenuComponent : Component
{
    [DataField("toggle", required: true)]
    public InstantAction ToggleAction = new InstantAction();
}
