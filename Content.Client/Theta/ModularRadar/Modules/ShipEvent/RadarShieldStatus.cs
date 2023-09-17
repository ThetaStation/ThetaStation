using System.Linq;
using System.Numerics;
using Content.Client.Theta.ShipEvent.Systems;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Theta.ShipEvent.UI;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarShieldStatus : RadarModule
{
    private readonly CircularShieldSystem _shieldSystem;
    private readonly TransformSystem _transformSystem;

    private List<ShieldInterfaceState> _shields = new();

    public RadarShieldStatus(ModularRadarControl parentRadar) : base(parentRadar)
    {
        _shieldSystem = EntManager.System<CircularShieldSystem>();
        _transformSystem = EntManager.System<TransformSystem>();
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        switch (state)
        {
            case RadarConsoleBoundInterfaceState radarState:
                _shields = radarState.Shields;
                break;
            case ShieldConsoleBoundsUserInterfaceState shieldState:
                _shields.Clear();
                _shields.Add(shieldState.Shield);
                break;
        }
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        base.Draw(handle, parameters);

        var ourGridId = ParentCoordinates!.Value.GetGridUid(EntManager);
        var rot = _transformSystem.GetWorldRotation(ourGridId!.Value);
        foreach (var state in _shields)
        {
            if(!state.Powered)
                continue;
            var position = EntManager.GetCoordinates(state.Coordinates).ToMapPos(EntManager);
            var color = Color.Blue;

            var cone = _shieldSystem.GenerateConeVertices(state.Radius, state.Angle, state.Width, 5);
            var verts = new Vector2[cone.Length + 1];
            for (var i = 0; i < cone.Length; i++)
            {
                verts[i] = cone[i];
                verts[i] = parameters.DrawMatrix.Transform(position + rot.RotateVec(verts[i]));
                verts[i].Y = -verts[i].Y;
                verts[i] = ScalePosition(verts[i]);
            }

            verts[cone.Length] = verts[0];

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color.WithAlpha(0.1f));
            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }

    // Only for shield console
    public void UpdateShieldParameters(Angle angle)
    {
        if(_shields.Count == 0)
            return;
        var state = _shields.First();
        state.Angle = angle;
    }
}
