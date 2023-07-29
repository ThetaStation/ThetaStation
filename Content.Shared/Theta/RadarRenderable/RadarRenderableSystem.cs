using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Shared.Theta.RadarRenderable;

public abstract class SharedRadarRenderableSystem : EntitySystem
{

}

[Flags]
public enum RadarRenderableGroup : uint
{
    Mob = 0,
    Projectiles = 1<<0,
    Cannon = 1<<1,
}


[Serializable, NetSerializable]
public sealed class SubscribeToRadarRenderableUpdatesEvent : EntityEventArgs
{
    public ICommonSession Subscriber;
    public RadarRenderableGroup GroupSubscriptions;

    public SubscribeToRadarRenderableUpdatesEvent(ICommonSession subscriber, RadarRenderableGroup groupSubscriptions)
    {
        Subscriber = subscriber;
        GroupSubscriptions = groupSubscriptions;
    }
}

[Serializable, NetSerializable]
public sealed class UnsubscribeRadarRenderableUpdatesEvent : EntityEventArgs
{
    public ICommonSession Subscriber;
    public RadarRenderableGroup GroupSubscriptions;

    public UnsubscribeRadarRenderableUpdatesEvent(ICommonSession subscriber, RadarRenderableGroup groupSubscriptions)
    {
        Subscriber = subscriber;
        GroupSubscriptions = groupSubscriptions;
    }
}

[Serializable, NetSerializable]
public sealed class RadarRenderableStatesEvent : EntityEventArgs
{
    public readonly List<CommonRadarEntityInterfaceState> States;

    public RadarRenderableStatesEvent(List<CommonRadarEntityInterfaceState> states)
    {
        States = states;
    }
}
