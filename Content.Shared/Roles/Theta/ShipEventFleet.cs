namespace Content.Shared.Roles.Theta;

public sealed class ShipEventFleet
{
    public string Name;
    public Color Color;
    public string? Admiral = null;
    public List<ShipEventTeam> Teams = new();

    public ShipEventFleet(string name, Color color, string? admiral)
    {
        Name = name;
        Color = color;
        Admiral = admiral;
    }
}