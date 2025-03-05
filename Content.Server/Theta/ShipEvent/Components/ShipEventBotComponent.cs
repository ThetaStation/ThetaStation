using System.Numerics;
using Content.Shared.Theta.ShipEvent;

namespace Content.Server.Theta.ShipEvent.Components;

[RegisterComponent]
public sealed partial class ShipEventBotComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PathfindingRange;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxFollowRange;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxAttackRange;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int FleeDamageThreshold;

    public TimeSpan LastUpdate = TimeSpan.Zero;
    public BotState State = BotState.Wander;
    public EntityUid? TargetUid = null;
    public int CurrentWaypoint;
    public List<Vector2> Waypoints = new();
    public List<Entity<CannonComponent>> CachedCannons = new();
}

public enum BotState
{
    Attack,
    Avoid,
    Wander
}
