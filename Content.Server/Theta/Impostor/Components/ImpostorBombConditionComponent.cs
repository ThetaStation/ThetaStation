namespace Content.Server.Theta.Impostor.Components;

[RegisterComponent]
public sealed partial class ImpostorBombConditionComponent : Component
{
    //Comparing by name instead of entity uid, since we might want to place several marks for a large room
    public string? TargetLandmarkName;
}
