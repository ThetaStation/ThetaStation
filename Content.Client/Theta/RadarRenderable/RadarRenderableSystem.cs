using System.Linq;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.RadarRenderable;
using Robust.Client.Player;

namespace Content.Client.Theta.RadarRenderable;

public sealed class RadarRenderableSystem : SharedRadarRenderableSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event Action? ClearOldStates;

    private event Action<List<CommonRadarEntityInterfaceState>>? SendMobStates;
    private event Action<List<CommonRadarEntityInterfaceState>>? SendProjectilesStates;
    private event Action<List<CommonRadarEntityInterfaceState>>? SendCannonStates;

    public override void Initialize()
    {
        SubscribeNetworkEvent<RadarRenderableStatesEvent>(OnStatesReceived);
    }

    private void OnStatesReceived(RadarRenderableStatesEvent ev)
    {
        ClearOldStates?.Invoke();

        var groups = ev.States.GroupBy(s => s.Group);
        foreach (var group in groups)
        {
            switch (group.Key)
            {
                case RadarRenderableGroup.Mob:
                    SendMobStates?.Invoke(group.ToList());
                    break;
                case RadarRenderableGroup.Projectiles:
                    SendProjectilesStates?.Invoke(group.ToList());
                    break;
                case RadarRenderableGroup.Cannon:
                    SendCannonStates?.Invoke(group.ToList());
                    break;
            }
        }
    }

    public void SubscribeUI(RadarRenderableGroup subscriptions, Action<List<CommonRadarEntityInterfaceState>> addStates)
    {
        if(_playerManager.LocalPlayer == null)
            return;

        if (subscriptions.HasFlag(RadarRenderableGroup.Mob))
        {
            SendMobStates += addStates;
        }
        else if(subscriptions.HasFlag(RadarRenderableGroup.Projectiles))
        {
            SendProjectilesStates += addStates;
        }
        else if(subscriptions.HasFlag(RadarRenderableGroup.Cannon))
        {
            SendCannonStates += addStates;
        }

        var session = _playerManager.LocalPlayer.Session;
        RaiseNetworkEvent(new SubscribeToRadarRenderableUpdatesEvent(session, subscriptions));
    }
}
