using System.Linq;
using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles.Theta;
using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventFactionSystem
{
    [Dependency] private readonly ExplosionSystem _expSys = default!;
    public bool BoundsCompression = false;
    public float BoundsCompressionInterval;
    public int BoundsCompressionDistance; //how much play area bounds are compressed after every BoundCompressionInterval
    public int CurrentBoundsOffset; //inward offset of bounds

    public Box2 GetPlayAreaBounds()
    {
        return new Box2i(
            CurrentBoundsOffset,
            CurrentBoundsOffset,
            MaxSpawnOffset - CurrentBoundsOffset,
            MaxSpawnOffset - CurrentBoundsOffset);
    }

    private void BoundsUpdate()
    {
        if (!BoundsCompression)
            return;

        CompressBounds();
    }

    public bool IsPositionOutOfBounds(Vector2 worldPos)
    {
        return !GetPlayAreaBounds().Contains(worldPos);
    }

    public bool IsTeamOutOfBounds(ShipEventFaction team)
    {
        if (!team.ShouldRespawn)
        {
            if (EntityManager.TryGetComponent<TransformComponent>(team.Ship, out var form) &&
                EntityManager.TryGetComponent<PhysicsComponent>(team.Ship, out var grid))
            {
                Matrix3 wmat = _formSys.GetWorldMatrix(form);
                return IsPositionOutOfBounds(wmat.Transform(grid.LocalCenter));
            }
        }

        return false;
    }

    //idk how to name it otherwise
    private void PunishOutOfBoundsTeam(ShipEventFaction team)
    {
        team.Points = Math.Max(0, team.Points - OutOfBoundsPenalty);
        var form = Transform(team.Ship);
        _expSys.QueueExplosion(Pick(form.ChildEntities), ExplosionSystem.DefaultExplosionPrototypeId, 4, 0.5f, 1);
    }

    public void CompressBounds()
    {
        CurrentBoundsOffset = Math.Min(MaxSpawnOffset / 2, CurrentBoundsOffset + BoundsCompressionDistance);
        UpdateBoundsOverlay();
    }

    private void UpdateBoundsOverlay(ICommonSession? recipient = null)
    {
        Box2 bounds = GetPlayAreaBounds();

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
        if (!RuleSelected)
            return;

        UpdateBoundsOverlay(sargs.SenderSession);
    }
}
