using System.Numerics;

[RegisterComponent]
public sealed partial class RocketConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? BoundLauncher;
}
