using System.Numerics;
using Content.Client.Theta.ShipEvent;
using Content.Shared.Theta.ShipEvent;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarControlCannons : RadarModule
{
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    private readonly CannonSystem _cannonSys = default!;

    private HashSet<EntityUid> _controlledCannons = new();
    private int _nextMouseHandle;
    private const int MouseCd = 20;

    public RadarControlCannons(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _cannonSys = EntManager.System<CannonSystem>();
        _cannonSys.CannonChangedEvent += OnCannonChanges;
        parentRadar.OnParentUidSet += OnParentSet;
    }

    private HashSet<EntityUid> GetAllControlledCannons()
    {
        HashSet<EntityUid> result = new();

        var query = EntManager.EntityQueryEnumerator<CannonComponent>();
        while (query.MoveNext(out var uid, out var cannon))
        {
            if (cannon.BoundConsoleUid == ParentUid)
                result.Add(uid);
        }

        return result;
    }

    private void OnCannonChanges(EntityUid uid, CannonComponent cannon)
    {
        if (cannon.LifeStage == ComponentLifeStage.Removing || cannon.BoundConsoleUid != ParentUid)
        {
            _controlledCannons.Remove(uid);
            return;
        }

        _controlledCannons.Add(uid);
    }

    private void OnParentSet()
    {
        _controlledCannons = GetAllControlledCannons();
    }

    public override void MouseMove(GUIMouseMoveEventArgs args)
    {
        if (_nextMouseHandle < MouseCd)
        {
            _nextMouseHandle++;
            return;
        }

        if (_controlledCannons.Count == 0)
            return;

        _nextMouseHandle = 0;
        RotateCannons(args.RelativePosition);
        args.Handle();
    }

    public override void OnKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.Use)
            return;

        if (_controlledCannons.Count == 0)
            return;

        var coordinates = RotateCannons(args.RelativePosition);

        var player = _playerMan.LocalSession?.AttachedEntity;
        if (player == null)
            return;

        var ev = new StartCannonFiringEvent(coordinates, player.Value);
        foreach (var entityUid in _controlledCannons)
        {
            EntManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }

        args.Handle();
    }

    public override void OnKeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.Use)
            return;

        if (_controlledCannons.Count == 0)
            return;

        var ev = new StopCannonFiringEventEvent();
        foreach (var entityUid in _controlledCannons)
        {
            EntManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }
    }

    private Vector2 RotateCannons(Vector2 mouseRelativePosition)
    {
        var offsetMatrix = OffsetMatrix;
        var relativePositionToCoordinates = RelativeToWorld(mouseRelativePosition, offsetMatrix);
        var player = _playerMan.LocalSession?.AttachedEntity;
        if (player == null)
            return relativePositionToCoordinates;
        foreach (var entityUid in _controlledCannons)
        {
            var ev = new RotateCannonEvent(relativePositionToCoordinates, player.Value);
            EntManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }

        return relativePositionToCoordinates;
    }
}
