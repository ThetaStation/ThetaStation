using System.Linq;
using System.Numerics;
using Content.Client.Theta.ShipEvent;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarControlCannons : RadarModule
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private List<CannonInformationInterfaceState> _cannons = new();

    private List<EntityUid> _controlledCannons = new();

    private int _nextMouseHandle;

    private const int MouseCd = 20;

    public RadarControlCannons(ModularRadarControl parentRadar) : base(parentRadar)
    {
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

        var player = _player.LocalPlayer?.ControlledEntity;
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
        var offsetMatrix = GetOffsetMatrix();
        var relativePositionToCoordinates = RelativePositionToCoordinates(mouseRelativePosition, offsetMatrix);
        var player = _player.LocalPlayer?.ControlledEntity;
        if (player == null)
            return relativePositionToCoordinates;
        foreach (var entityUid in _controlledCannons)
        {
            var ev = new RotateCannonEvent(relativePositionToCoordinates, player.Value);
            EntManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }

        return relativePositionToCoordinates;
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not RadarConsoleBoundInterfaceState radarState)
            return;

        _cannons = radarState.Cannons;
        _controlledCannons = _cannons
            .Where(i => i.IsControlling)
            .Select(i => i.Uid)
            .ToList();
    }
}
