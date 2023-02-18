using Content.Client.Shuttles.UI;
using Content.Shared.Theta.ShipEvent.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using TerraFX.Interop.Windows;

namespace Content.Client.Theta.ShipEvent.Console;

public sealed class CannonRadarControl : RadarControl
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private List<EntityUid> _controlledCannons = new();

    private int _mouseCd;

    public CannonRadarControl() : base()
    {
        OnKeyBindDown += StartFiring;
        OnKeyBindUp += StopFiring;
    }

    public void UpdateControlledCannons(CannonConsoleBoundInterfaceState state)
    {
        _controlledCannons = state.ControlledCannons;
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);
        if (_mouseCd < 20)
        {
            _mouseCd++;
            return;
        }

        _mouseCd = 0;
        RotateCannons(args.RelativePosition);
        args.Handle();
    }

    private void StartFiring(GUIBoundKeyEventArgs args)
    {
        if(args.Function != EngineKeyFunctions.Use)
            return;
        var coordinates = RotateCannons(args.RelativePosition);

        var player = _player.LocalPlayer?.ControlledEntity;
        if(player == null)
            return;
        var ev = new StartCannonFiringEvent(coordinates, player.Value);
        foreach (var entityUid in _controlledCannons)
        {
            _entManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }

        args.Handle();
    }

    private void StopFiring(GUIBoundKeyEventArgs args)
    {
        if(args.Function != EngineKeyFunctions.Use)
            return;
        var ev = new StopCannonFiringEventEvent();
        foreach (var entityUid in _controlledCannons)
        {
            _entManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }
    }

    private Vector2 RotateCannons(Vector2 mouseRelativePosition)
    {
        var offsetMatrix = GetOffsetMatrix();
        var relativePositionToCoordinates = RelativePositionToCoordinates(mouseRelativePosition, offsetMatrix);
        var player = _player.LocalPlayer?.ControlledEntity;
        if(player == null)
            return relativePositionToCoordinates;
        foreach (var entityUid in _controlledCannons)
        {
            var ev = new RotateCannonEvent(relativePositionToCoordinates, player.Value);
            _entManager.EventBus.RaiseLocalEvent(entityUid, ref ev);
        }
        return relativePositionToCoordinates;
    }

    private Vector2 RelativePositionToCoordinates(Vector2 pos, Matrix3 matrix)
    {
        var removeScale = (pos - MidPoint) / MinimapScale;
        removeScale.Y = -removeScale.Y;
        return matrix.Transform(removeScale);
    }

    protected override Color GetCannonColor(EntityUid cannon)
    {
        return _controlledCannons.Contains(cannon) ? Color.Lime : Color.LightGreen;
    }

}
