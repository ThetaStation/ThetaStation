using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.RadarRenderable;
using Robust.Shared.Players;

namespace Content.Server.Theta.RadarRenderable;

public sealed class RadarRenderableSystem : SharedRadarRenderableSystem
{
    private readonly Dictionary<ICommonSession, float> _subscriberBySession = new();
    private readonly Dictionary<ICommonSession, RadarRenderableGroup> _subscriberBySubscriptionGroups = new();

    public override void Initialize()
    {
        SubscribeNetworkEvent<SubscribeToRadarRenderableUpdatesEvent>(OnSubscribeUI);
        SubscribeNetworkEvent<UnsubscribeRadarRenderableUpdatesEvent>(OnUnsubscribeUI);
    }

    private void OnSubscribeUI(SubscribeToRadarRenderableUpdatesEvent ev)
    {
        if (_subscriberBySession.ContainsKey(ev.Subscriber))
        {
            _subscriberBySession[ev.Subscriber] += 1;
            _subscriberBySubscriptionGroups[ev.Subscriber] |= ev.GroupSubscriptions;
            return;
        }

        _subscriberBySession.Add(ev.Subscriber, 1);
        _subscriberBySubscriptionGroups.Add(ev.Subscriber, ev.GroupSubscriptions);
    }

    private void OnUnsubscribeUI(UnsubscribeRadarRenderableUpdatesEvent ev)
    {
        if (_subscriberBySession.ContainsKey(ev.Subscriber))
        {
            _subscriberBySession[ev.Subscriber] -= 1;
            _subscriberBySubscriptionGroups[ev.Subscriber] ^= ev.GroupSubscriptions;
        }

        if (_subscriberBySession[ev.Subscriber] == 0)
        {
            _subscriberBySession.Remove(ev.Subscriber);
            _subscriberBySubscriptionGroups.Remove(ev.Subscriber);
        }
    }

    public override void Update(float frameTime)
    {
        if (_subscriberBySession.Count == 0)
            return;
        var states = new List<CommonRadarEntityInterfaceState>();
        // TODO:
    }
}
