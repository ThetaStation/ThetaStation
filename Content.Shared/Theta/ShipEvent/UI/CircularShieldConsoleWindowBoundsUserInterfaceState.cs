using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class CircularShieldConsoleWindowBoundsUserInterfaceState : BoundUserInterfaceState
{
    public bool Enabled;
    public bool Powered;

    public int Angle;

    public int Width;
    public int MaxWidth;

    public int Radius;
    public int MaxRadius;

    public CircularShieldConsoleWindowBoundsUserInterfaceState(
        bool enabled, 
        bool powered, 
        int angle, 
        int width, 
        int maxWidth, 
        int radius, 
        int maxRadius)
    {
        Enabled = enabled;
        Powered = powered;
        Angle = angle;
        Width = width;
        MaxWidth = maxWidth;
        Radius = radius;
        MaxRadius = maxRadius;
    }
}

[Serializable, NetSerializable]
public sealed class CircularShieldConsoleInfoRequest : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public enum CircularShieldConsoleUiKey
{
    Key
}

