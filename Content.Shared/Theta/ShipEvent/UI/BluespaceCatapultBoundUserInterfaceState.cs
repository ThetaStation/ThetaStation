using Robust.Shared.Serialization;

namespace Content.Shared.Theta.ShipEvent.UI;

[Serializable, NetSerializable]
public sealed class BluespaceCatapultBoundUserInterfaceState : BoundUserInterfaceState
{
    public float Charge;
    public float MaxCharge;
    public string UserMessage = "";

    public BluespaceCatapultBoundUserInterfaceState(float charge, float maxCharge, string userMessage)
    {
        Charge = charge;
        MaxCharge = maxCharge;
        UserMessage = userMessage;
    }
}

[Serializable, NetSerializable]
public sealed class BluespaceCatapultLaunchRequest : BoundUserInterfaceMessage
{
    public int Elevation;
    public int Bearing;
    public int Power;

    public BluespaceCatapultLaunchRequest(int elevation, int bearing, int power)
    {
        Elevation = elevation;
        Bearing = bearing;
        Power = power;
    }
}

[Serializable, NetSerializable]
public sealed class BluespaceCatapultRefreshRequest : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public enum BluespaceCatapultUiKey
{
    Key
}
