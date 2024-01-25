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
    public readonly List<DockingInterfaceState> Docks; //todo (radars): docks are only required for the shuttle console; move em outta here
    public readonly List<CommonRadarEntityInterfaceState> CommonEntities;

    //todo (radars): we are already sending all the data we need for the radar's UI, by dirtying cannons, shields, and other stuff,
    //yet we redundantly send those BUI states. we need to come up with a way to separate shield, cannon and shuttle console windows
    //functionality into something like radar modules, and force them to use clients comp data
    //...or atleast remove docks from this state and move it to shuttle console
    public RadarConsoleBoundInterfaceState(
        float maxRange,
        NetCoordinates? coordinates,
        Angle? angle,
        List<DockingInterfaceState> docks,
        List<CommonRadarEntityInterfaceState> common)
    {
        MaxRange = maxRange;
        Coordinates = coordinates;
        Angle = angle;
        Docks = docks;
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
    public int MaxAmmo;
}

[Serializable, NetSerializable]
public sealed class ShieldInterfaceState
{
    public NetCoordinates Coordinates;
    public bool Powered;
    public Angle Angle;
    public Angle Width;
    public Angle MaxWidth;
    public int Radius;
    public int MaxRadius;
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
