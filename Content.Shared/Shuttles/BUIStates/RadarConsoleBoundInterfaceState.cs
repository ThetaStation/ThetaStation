using Content.Shared.Theta.RadarRenderable;
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

    public readonly List<CannonInformationInterfaceState> Cannons;

    public readonly List<CommonRadarEntityInterfaceState> CommonEntities;

    public RadarConsoleBoundInterfaceState(
        float maxRange,
        EntityCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<CannonInformationInterfaceState> cannons,
        List<CommonRadarEntityInterfaceState> common)
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Docks = docks;
        Cannons = cannons;
        CommonEntities = common;
    }
}

[Serializable, NetSerializable]
public sealed class CommonRadarEntityInterfaceState
{
    public EntityCoordinates Coordinates;
    public Angle Angle;
    public string RadarViewPrototype;
    public Color? OverrideColor;

    public CommonRadarEntityInterfaceState(EntityCoordinates coordinates, Angle angle, string radarViewPrototype,
        Color? color = null)
    {
        Coordinates = coordinates;
        Angle = angle;
        RadarViewPrototype = radarViewPrototype;
        OverrideColor = color;
    }
}

[Flags]
[Serializable, NetSerializable]
public enum RadarRenderableGroup
{
    Mob = 0,
    Projectiles = 1 << 0,
    Cannon = 1 << 1,

    All = (Mob | Projectiles | Cannon),
}

/// <summary>
/// State of each cannon on shuttle grid
/// </summary>
[Serializable, NetSerializable]
public sealed class CannonInformationInterfaceState
{
    public EntityUid Uid;
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
