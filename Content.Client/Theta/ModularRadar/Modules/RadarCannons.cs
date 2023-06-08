using System.Linq;
using Content.Client.Theta.ShipEvent;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Input;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarCannons : RadarModule
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private List<CannonInformationInterfaceState> _cannons = new();

    private List<EntityUid> _controlledCannons = new();

    private int _nextMouseHandle;

    private const int MouseCd = 20;

    public RadarCannons(ModularRadarControl parentRadar) : base(parentRadar)
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

    public override void OnKeyBindUp(GUIBoundKeyEventArgs args)
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

    public override void OnKeyBindDown(GUIBoundKeyEventArgs args)
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

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        const float cannonSize = 3f;
        foreach (var cannon in _cannons)
        {
            var position = cannon.Coordinates.ToMapPos(EntManager);
            var angle = cannon.Angle;
            var color = cannon.Color;

            var hsvColor = Color.ToHsv(color);

            const float additionalDegreeCoeff = 20f / 360f;

            // X is hue
            var hueOffset = hsvColor.X * cannon.UsedCapacity / Math.Max(1, cannon.MaxCapacity);
            hsvColor.X = Math.Max(hueOffset + additionalDegreeCoeff, additionalDegreeCoeff);

            color = Color.FromHsv(hsvColor);

            var matrix = parameters.DrawMatrix;
            var verts = new[]
            {
                matrix.Transform(position + angle.RotateVec(new Vector2(-cannonSize / 2, cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(0, -cannonSize / 2 - cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(cannonSize / 2, cannonSize / 4))),
                matrix.Transform(position + angle.RotateVec(new Vector2(-cannonSize / 2, cannonSize / 4))),
            };
            for (var i = 0; i < verts.Length; i++)
            {
                var vert = verts[i];
                vert.Y = -vert.Y;
                verts[i] = ScalePosition(vert);
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }
}
