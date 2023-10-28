namespace Content.Server.Theta.Impostor.Components;

//todo: this should be an enum, but vv is broken and instead of showing a dropdown list like it should, it displays it as a readonly string
//since mappers need to edit it thru vv, it was remade into static class
public static class ImpostorLandmarkType
{
    public const string Unspecified = "unspec";
    public const string ImpostorLootStash = "loot";
    public const string ImpostorBombLocation = "bomb";
    public const string EvacPod = "pod";
}

/// <summary>
/// Used for marking various locations on the station
/// </summary>
[RegisterComponent]
public sealed partial class ImpostorLandmarkComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Type = ImpostorLandmarkType.Unspecified;
}
