using System.Numerics;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Roles.Theta;
using Content.Shared.Theta.ShipEvent;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;

namespace Content.Server.Theta.ShipEvent.Systems;

public sealed partial class ShipEventTeamSystem
{
    [Dependency] private readonly ExplosionSystem _expSys = default!;
    public bool BoundsCompression = false;
    public float BoundsCompressionInterval;
    public int BoundsCompressionDistance; //how much play area bounds are compressed after every BoundCompressionInterval

    private void BoundsUpdate()
    {
        if (!BoundsCompression)
            return;

        CompressBounds();
    }

    public bool IsPositionInBounds(Vector2 worldPos)
    {
        return PlayArea.Contains(worldPos);
    }

    public bool IsTeamInBounds(ShipEventTeam team)
    {
        if (!team.QueuedForRespawn)
        {
            if (EntityManager.TryGetComponent<TransformComponent>(team.ShipMainGrid, out var form) &&
                EntityManager.TryGetComponent<PhysicsComponent>(team.ShipMainGrid, out var grid))
            {
                return IsPositionInBounds(grid.LocalCenter + _formSys.GetWorldPosition(form));
            }
        }

        return true;
    }

    //idk how to name it otherwise
    private void PunishOutOfBoundsTeam(ShipEventTeam team)
    {
        team.Points = Math.Max(0, team.Points - OutOfBoundsPenalty);
        var form = Transform(team.ShipMainGrid!.Value);
        _expSys.QueueExplosion(Pick(form.ChildEntities), ExplosionSystem.DefaultExplosionPrototypeId, 5, 0.5f, 1);
    }

    public void CompressBounds()
    {
        PlayArea.BottomLeft += new Vector2(BoundsCompressionDistance);
        PlayArea.TopRight -= new Vector2(BoundsCompressionDistance);
        UpdateBoundsOverlay();
    }

    private void UpdateBoundsOverlay(ICommonSession? recipient = null)
    {
        if (recipient == null)
        {
            RaiseNetworkEvent(new BoundsOverlayInfo(TargetMap, PlayArea));
        }
        else
        {
            RaiseNetworkEvent(new BoundsOverlayInfo(TargetMap, PlayArea), recipient);
        }
    }

    private void OnBoundsOverlayInfoRequest(BoundsOverlayInfoRequest args, EntitySessionEventArgs sargs)
    {
        if (!RuleSelected)
            return;

        UpdateBoundsOverlay(sargs.SenderSession);
    }
}
