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

    public readonly List<CannonInformationInterfaceState> Cannons;

    public readonly List<CommonRadarEntityInterfaceState> All;

    public RadarConsoleBoundInterfaceState(
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<MobInterfaceState> mobs,
        List<ProjectilesInterfaceState> projectiles,
        List<CannonInformationInterfaceState> cannons,
        List<CommonRadarEntityInterfaceState> all)
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Docks = docks;
        MobsAround = mobs;
        Projectiles = projectiles;
        Cannons = cannons;
        All = all;
    }
}

[Serializable, NetSerializable]
public sealed class CommonRadarEntityInterfaceState
{
    public EntityCoordinates Coordinates;
    public Angle Angle;
    public string RadarViewPrototype;
    public Color? OverrideColor;

    public CommonRadarEntityInterfaceState(EntityCoordinates coordinates, Angle angle, string radarViewPrototype, Color? color = null)
    {
        Coordinates = coordinates;
        Angle = angle;
        RadarViewPrototype = radarViewPrototype;
    }
}

/// <summary>
/// State of each mobs around radar
/// </summary>
[Serializable, NetSerializable]
public sealed class ProjectilesInterfaceState
{
    public EntityCoordinates Coordinates;
    public Angle Angle;
}

/// <summary>
/// State of each projectile around radar
/// </summary>
[Serializable, NetSerializable]
public sealed class MobInterfaceState
{
    public EntityCoordinates Coordinates;
}

/// <summary>
/// State of each cannon on shuttle grid
/// </summary>
[Serializable, NetSerializable]
public sealed class CannonInformationInterfaceState
{
    public EntityUid Uid;
    public EntityCoordinates Coordinates;
    public Color Color;
    public Angle Angle;
    public bool IsControlling;
    public int Ammo;
    public int MaxCapacity;
    public int UsedCapacity;
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
