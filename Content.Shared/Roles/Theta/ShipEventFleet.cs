namespace Content.Shared.Roles.Theta;

/// <summary>
/// A collection of teams, controlled by the admiral (may not be present).
/// Most of the fields in this class should only be set by the methods inside the team system, 
/// I'm just too lazy to implement proper access restrictions. So please take a look at the team system before touching anything.
/// </summary>
public sealed class ShipEventFleet
{
    public string Name;
    public Color Color;
    public string? Admiral = null;
    public bool AdmiralLocked; //if true new admiral won't be selected when the old one disconnects
    public List<ShipEventTeam> Teams = new();

    public ShipEventFleet(string name, Color color, string? admiral)
    {
        Name = name;
        Color = color;
        Admiral = admiral;
    }
}