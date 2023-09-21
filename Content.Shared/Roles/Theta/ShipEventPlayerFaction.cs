using Content.Shared.Theta.ShipEvent;

namespace Content.Shared.Roles.Theta;

public sealed class ShipEventFaction : PlayerFaction
{
    public string Captain; //ckey

    public Color Color;

    public Dictionary<ShipEventFaction, int> Hits = new(); //hits from other teams, not vice-versa
    public int Kills;
    public int Assists;
    public int Points;
    public int Respawns;

    public ShipTypePrototype? ChosenShipType;
    public EntityUid Ship;
    public string ShipName = "";

    public bool ShouldRespawn; //whether this team is currently waiting for respawn
    public float TimeSinceRemoval; //time since last removal

    public bool OutOfBoundsWarningReceived; //whether this team has already received warning about going out of play area

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

    public ShipEventFaction(string name, string iconPath, Color color, string captain,
        int points = 0) : base(name, iconPath)
    {
        Color = color;
        Captain = captain;
        Points = points;
    }
}
