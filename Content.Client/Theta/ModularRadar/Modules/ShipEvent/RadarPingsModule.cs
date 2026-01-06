using System.Numerics;
using Content.Client.Theta.RadarPings;
using Content.Shared.Input;
using Content.Shared.Theta.RadarPings;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarPingsModule : RadarModule
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private readonly RadarPingsSystem _radarPingsSystem;
    private readonly List<AnimationPingInformation> _pingsOnRender = new();

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

        var offsetMatrix = OffsetMatrix;
        var relativePositionToCoordinates = RelativeToWorld(mouseRelativePosition, offsetMatrix);

        _radarPingsSystem.SendPing(ParentUid, relativePositionToCoordinates);
    }

    private void AddPing(PingInformation ping)
    {
        _pingsOnRender.Add(new AnimationPingInformation(ping.Coordinates, ping.Color));
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        if (_pingsOnRender.Count == 0)
            return;

        List<AnimationPingInformation> pingsToRemove = new();
        foreach (var ping in _pingsOnRender)
        {
            if (ping.StartAnimationTick == TimeSpan.Zero)
                ping.StartAnimationTick = _gameTiming.RealTime;

            if (ping.StartAnimationTick + ping.PingDuration < _gameTiming.RealTime)
            {
                pingsToRemove.Add(ping);
                continue;
            }

            var coordinates = ping.Coordinates;
            var uiPosition = Vector2.Transform(coordinates, parameters.DrawMatrix);
            uiPosition.Y = -uiPosition.Y;

            handle.DrawCircle(ScalePosition(uiPosition), ping.DotRadius, ping.Color);

            var animationTick = (_gameTiming.RealTime - ping.StartAnimationTick) / (ping.PingDuration / 5);
            var coefficient = (float)Math.Abs(Math.Sin(animationTick));
            var radiusDelta = ping.MaxCircleRadius - ping.MinCircleRadius;

            handle.DrawCircle(ScalePosition(uiPosition), ping.MinCircleRadius + coefficient * radiusDelta, ping.Color, false);
        }

        foreach (var ping in pingsToRemove)
        {
            _pingsOnRender.Remove(ping);
        }
    }

    private sealed class AnimationPingInformation
    {
        public readonly Vector2 Coordinates;
        public readonly Color Color;

        public TimeSpan StartAnimationTick = TimeSpan.Zero;

        public float DotRadius = 3f;
        public float MinCircleRadius = 7f;
        public float MaxCircleRadius = 11f;

        public TimeSpan PingDuration = TimeSpan.FromSeconds(2);

        public AnimationPingInformation(Vector2 coordinates, Color color)
        {
            Coordinates = coordinates;
            Color = color;
        }
    }
}
