using System.Numerics;
using Content.Client.Theta.RadarPings;
using Content.Shared.Input;
using Content.Shared.Theta.RadarPings;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarPingsModule : RadarModule
{
    private readonly RadarPingsSystem _radarPingsSystem;
    private readonly List<PingInformation> _pingsOnRender = new();

    public RadarPingsModule(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _radarPingsSystem = EntManager.System<RadarPingsSystem>();
        _radarPingsSystem.OnEventReceived += AddPing;
    }

    public override void OnKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function != ContentKeyFunctions.PingOnRadar)
            return;

        SendPing(args.RelativePosition);

        args.Handle();
    }

    private void SendPing(Vector2 mouseRelativePosition)
    {
        if (ParentCoordinates == null)
            return;

        var offsetMatrix = GetOffsetMatrix();
        var relativePositionToCoordinates = RelativePositionToCoordinates(mouseRelativePosition, offsetMatrix);

        var ping = _radarPingsSystem.SendPing(OwnerUid, relativePositionToCoordinates);
        AddPing(ping);
    }

    private void AddPing(PingInformation ping)
    {
        _pingsOnRender.Add(ping);
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        if (_pingsOnRender.Count == 0)
            return;

        foreach (var ping in _pingsOnRender)
        {
            var coordinates = ping.Coordinates;
            var uiPosition = parameters.DrawMatrix.Transform(coordinates);
            uiPosition.Y = -uiPosition.Y;

            handle.DrawCircle(ScalePosition(uiPosition), 2f, ping.Color);
            handle.DrawCircle(ScalePosition(uiPosition), 5f, ping.Color, false);
        }
    }
}
