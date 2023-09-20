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
    public NetCoordinates? Coordinates;

    /// <summary>
    /// The relevant rotation to rotate the angle around.
    /// </summary>
    public Angle? Angle;

    public readonly List<DockingInterfaceState> Docks;

    public readonly List<CannonInformationInterfaceState> Cannons;

    public readonly List<DoorInterfaceState> Doors;

    public readonly List<CommonRadarEntityInterfaceState> CommonEntities;

    public readonly List<ShieldInterfaceState> Shields;

    public RadarConsoleBoundInterfaceState(
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<CannonInformationInterfaceState> cannons,
        List<DoorInterfaceState> doors,
        List<CommonRadarEntityInterfaceState> common,
        List<ShieldInterfaceState> shields)
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Docks = docks;
        Cannons = cannons;
        Doors = doors;
        Shields = shields;
        CommonEntities = common;
    }
}

[Serializable, NetSerializable]
public sealed class CommonRadarEntityInterfaceState
{
    public NetCoordinates Coordinates;
    public Angle Angle;
    public string RadarViewPrototype;
    public Color? OverrideColor;

    public CommonRadarEntityInterfaceState(NetCoordinates coordinates, Angle angle, string radarViewPrototype,
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
    None                   =      0,
    ShipEventTeammate      = 1 << 0,
    Projectiles            = 1 << 1,
    Cannon                 = 1 << 2,
    Door                   = 1 << 3,
    Pickup                 = 1 << 4,

    All = (ShipEventTeammate | Projectiles | Cannon | Door | Pickup),
}

/// <summary>
/// State of each cannon on shuttle grid
/// </summary>
[Serializable, NetSerializable]
public sealed class CannonInformationInterfaceState
{
    public NetEntity Uid;
    public bool IsControlling;
    public int Ammo;
    public int MaxCapacity;
    public int UsedCapacity;
}

[Serializable, NetSerializable]
public sealed class ShieldInterfaceState
{
    public NetCoordinates Coordinates;
    public Angle WorldRotation;

    public bool Powered;

    public Angle Angle;

    public Angle Width;
    public int MaxWidth;

    public int Radius;
    public int MaxRadius;

    public bool IsControlling;
}

/// <summary>
/// State of each individual docking port for interface purposes
/// </summary>
[Serializable, NetSerializable]
public sealed class DockingInterfaceState
{
    public NetCoordinates Coordinates;
    public Angle Angle;
    public NetEntity Entity;
    public bool Connected;
    public Color Color;
    public Color HighlightedColor;
}

/// <summary>
/// State of each door on shuttle grid
/// </summary>
[Serializable, NetSerializable]
public sealed class DoorInterfaceState
{
    public NetEntity Uid;
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}
