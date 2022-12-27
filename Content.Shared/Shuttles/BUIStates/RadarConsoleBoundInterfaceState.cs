using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
[Virtual]
public class RadarConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public readonly float MaxRange;

    /// <summary>
    /// The relevant coordinates to base the radar around.
    /// </summary>
    public EntityCoordinates? Coordinates;

    /// <summary>
    /// The relevant rotation to rotate the angle around.
    /// </summary>
    public Angle? Angle;

    public readonly List<DockingInterfaceState> Docks;

    public readonly List<MobInterfaceState> MobsAround;

    public readonly List<ProjectilesInterfaceState> Projectiles;

    public RadarConsoleBoundInterfaceState(
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<MobInterfaceState> mobs,
        List<ProjectilesInterfaceState> projectiles
        )
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Docks = docks;
        MobsAround = mobs;
        Projectiles = projectiles;
    }
}

/// <summary>
/// State of each mobs around radar
/// </summary>
[Serializable, NetSerializable]
public sealed class ProjectilesInterfaceState
{
    public EntityCoordinates Coordinates;
    public EntityUid Entity;
    public Angle Angle;
}

/// <summary>
/// State of each projectile around radar
/// </summary>
[Serializable, NetSerializable]
public sealed class MobInterfaceState
{
    public EntityCoordinates Coordinates;
    public EntityUid Entity;
}

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class DockingInterfaceState
{
    public EntityCoordinates Coordinates;
    public Angle Angle;
    public EntityUid Entity;
    public bool Connected;
    public Color Color;
    public Color HighlightedColor;
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}
