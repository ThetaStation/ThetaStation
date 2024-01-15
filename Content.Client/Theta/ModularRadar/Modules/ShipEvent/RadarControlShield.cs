using System.Linq;
using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarControlShield : RadarModule
{
    public event Action<Angle>? UpdateShieldRotation;

    private readonly List<ShieldInterfaceState> _shields = new();

    private bool _holdLmb = false;

    public RadarControlShield(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not ShieldConsoleBoundsUserInterfaceState shieldState)
            return;
        _shields.Clear();
        _shields.Add(shieldState.Shield);
    }

    public override void MouseMove(GUIMouseMoveEventArgs args)
    {
        if (_shields.Count == 0)
            return;
        if(_holdLmb)
            RotateShields(args.RelativePosition);
        args.Handle();
    }
    public override void OnKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.Use)
            return;

        if (_shields.Count == 0)
            return;

        RotateShields(args.RelativePosition);
        _holdLmb = true;
        args.Handle();
    }

    public override void OnKeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.Use)
            return;

        if (_shields.Count == 0)
            return;

        _holdLmb = false;
        args.Handle();
    }


    private void RotateShields(Vector2 mouseRelativePosition)
    {
        var mouseCoords = InverseScalePosition(mouseRelativePosition).Normalized();
        mouseCoords.Y = -mouseCoords.Y;
        var unitVector = Vector2.UnitX;

        var dot = Vector2.Dot(mouseCoords, unitVector);
        var newAngle = Math.Acos(dot);
        var angle = new Angle(newAngle);
        if (mouseCoords.Y < 0)
            angle = 2 * Math.PI - angle;
        UpdateShieldRotation?.Invoke(angle);
    }
}
