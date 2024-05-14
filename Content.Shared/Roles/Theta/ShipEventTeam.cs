using Content.Shared.Theta.ShipEvent;

namespace Content.Shared.Roles.Theta;

public sealed class ShipEventTeam
{
    public string Name;
    public Color Color;
    public string? Captain;
    public List<string> Members = new();

    public Dictionary<ShipEventTeam, int> Hits = new(); //hits from other teams, not vice-versa
    public int Kills;
    public int Assists;
    public int Points;
    public int Respawns;

    public ShipTypePrototype? ChosenShipType;
    public List<EntityUid> ShipGrids = new();
    public EntityUid? ShipMainGrid;
    public string ShipName = "";

    public bool ShouldRespawn; //whether this team is currently waiting for respawn
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

    public ShipEventTeam(string name, Color color, string? captain, string? password = null)
    {
        Name = name;
        Color = color;
        Captain = captain;
        _password = password;
    }
}
