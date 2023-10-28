namespace Content.Server.Theta.Impostor.Components;

[Serializable]
public enum ImpostorLandmarkType
{
    Unspecified,
    ImpostorLootStash,
    ImpostorBombLocation,
    EvacPod,
    Bridge
}

/// <summary>
/// Used for marking various locations on the station
/// </summary>
[RegisterComponent]
public sealed partial class ImpostorLandmarkComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)] 
    public ImpostorLandmarkType Type = ImpostorLandmarkType.Unspecified;
}
