using Content.Server.Explosion.EntitySystems;
using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Players;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem
{
    [Dependency] private readonly ExplosionSystem _expSys = default!;

    private void CheckBoundsCompressionTimer()
    {
        if (!BoundsCompression)
            return;
        
        if (_boundsCompressionTimer > BoundsCompressionInterval)
        {
            _boundsCompressionTimer -= BoundsCompressionInterval;
            CompressBounds();
        }
    }

    private bool IsTeamOutOfBounds(ShipEventFaction team)
    {
        if (!team.ShouldRespawn)
        {
            if (EntityManager.TryGetComponent<TransformComponent>(team.Ship, out var form) && 
                EntityManager.TryGetComponent<PhysicsComponent>(team.Ship, out var grid))
            {
                Box2 bounds = new Box2(
                    MaxSpawnOffset - CurrentBoundsSize, 
                    MaxSpawnOffset - CurrentBoundsSize, 
                    CurrentBoundsSize, 
                    CurrentBoundsSize);
                Matrix3 wmat = _formSys.GetWorldMatrix(form);
                return !bounds.Contains(wmat.Transform(grid.LocalCenter));
            }
        }

        return false;
    }

    //idk how to name it otherwise
    private void PunishOutOfBoundsTeam(ShipEventFaction team)
    {
        team.Points -= OutOfBoundsPenalty;
        team.Points = team.Points < 0 ? 0 : team.Points;

        Vector2 teamShipPos = _formSys.GetWorldPosition(Transform(team.Ship));
        MapCoordinates mapCoords = new MapCoordinates(teamShipPos + _random.NextVector2(5), TargetMap);
        _expSys.QueueExplosion(mapCoords, ExplosionSystem.DefaultExplosionPrototypeId, 100, 5, 50);
    }

    private void CompressBounds()
    {
        Announce(Loc.GetString("shipevent-boundscompressed", ("distance", BoundsCompressionDistance)));
        CurrentBoundsSize -= BoundsCompressionDistance;
        CurrentBoundsSize = CurrentBoundsSize < 0 ? 0 : CurrentBoundsSize;
        UpdateBoundsOverlay();
    }

    private void UpdateBoundsOverlay(ICommonSession? recipient = null)
    {
        Box2i bounds = new Box2i(
            MaxSpawnOffset - CurrentBoundsSize, 
            MaxSpawnOffset - CurrentBoundsSize, 
            CurrentBoundsSize, 
            CurrentBoundsSize);
        
        if (recipient == null)
        {
            RaiseNetworkEvent(new BoundsOverlayInfo(TargetMap, bounds));
        }
        else
        {
            RaiseNetworkEvent(new BoundsOverlayInfo(TargetMap, bounds), recipient);
        }
    }

    private void OnBoundsOverlayInfoRequest(BoundsOverlayInfoRequest args, EntitySessionEventArgs sargs)
    {
        UpdateBoundsOverlay(sargs.SenderSession);
    }
}
