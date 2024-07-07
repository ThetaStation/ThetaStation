namespace Content.Server.Theta.ShipEvent.Console;

[RegisterComponent]
public sealed partial class CannonConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> BoundCannonUids = new();
}
