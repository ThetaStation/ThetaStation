using Content.Shared.Theta.ShipEvent;

namespace Content.Shared.Roles.Theta;

/// <summary>
/// A single ship and it's crew, controlled by the captain (may not be present).
/// Most of the fields in this class should only be set by the methods inside the team system (captain, fleet, ship grids, members, etc.), 
/// I'm just too lazy to implement proper access restrictions. So please take a look at the team system before touching anything.
/// </summary>
public sealed class ShipEventTeam
{
    public string Name = string.Empty;
    public Color Color;
    public string? Captain;
    public bool CaptainLocked; //if true new captain won't be selected when the old one disconnects
    public List<string> Members = new();
    public ShipEventFleet? Fleet = null;

    public Dictionary<ShipEventTeam, int> Hits = new(); //hits from other teams, not vice-versa
    public int Kills;
    public int Assists;
    public int Points;
    public int Respawns;

    public ShipTypePrototype? ChosenShipType;
    public List<EntityUid> ShipGrids = new();
    public EntityUid? ShipMainGrid;
    public string ShipName = "";

    public bool QueuedForRespawn; //whether this team is currently waiting for respawn
    public float TimeSinceRemoval;
    public bool OutOfBoundsWarningReceived;
    public int LastBonusInterval; //how much times this team has acquired bonus points for surviving bonus interval

    private string? _password;
    public string? JoinPassword
    {
        get => _password;
        set => _password = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private int _maxMembers;

    public int MaxMembers
    {
        get => _maxMembers;
        set => _maxMembers = int.Clamp(value, 0, 100);
    }
}
