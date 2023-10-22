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
public sealed partial class ImpostorLocationMarkerComponent : Component
{
    [DataField] public ImpostorLandmarkType Type = ImpostorLandmarkType.Unspecified;
}
