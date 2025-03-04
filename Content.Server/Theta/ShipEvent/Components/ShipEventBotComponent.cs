using System.Numerics;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipEventBotComponent : Component
{
    [DataField]
    public int PathfindingRange;

    [DataField]
    public int MaxFollowRange;

    [DataField]
    public int MaxAttackRange;

    [DataField]
    public int FleeDamageThreshold;

    public TimeSpan LastUpdate = TimeSpan.Zero;
    public BotState State = BotState.Wander;
    public EntityUid? TargetUid;
    public int CurrentWaypoint;
    public List<Vector2> Waypoints;
}

public enum BotState
{
    Attack,
    Avoid,
    Wander
}
