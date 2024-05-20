using System.Numerics;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Client.Theta.ModularRadar.Modules;

public sealed class RadarDocks : RadarModule
{
    private Dictionary<NetEntity, List<DockingPortState>> _docks = new();

    public bool ShowDocks { get; set; } = true;

    public RadarDocks(ModularRadarControl parentRadar) : base(parentRadar)
    {
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if(state is not RadarConsoleBoundInterfaceState radarState)
            return;

        _docks = radarState.DockState.Docks;
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        // Draw our grid in detail
        var fixturesQuery = EntManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = EntManager.GetEntityQuery<PhysicsComponent>();

        var ourGridId = ParentCoordinates!.Value.GetGridUid(EntManager);
        if(ourGridId == null)
            return;

        if (EntManager.HasComponent<MapGridComponent>(ourGridId) &&
            fixturesQuery.HasComponent(ourGridId.Value))
        {
            DrawDocks(handle, ourGridId.Value, parameters);
        }

        var mapPosition = ParentCoordinates.Value.ToMap(EntManager);

        foreach (var grid in MapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - MaxRadarRangeVector, mapPosition.Position + MaxRadarRangeVector)))
        {
            if (grid.Owner == ourGridId || !fixturesQuery.HasComponent(grid.Owner))
                continue;

            var gridBody = bodyQuery.GetComponent(grid.Owner);
            if (gridBody.Mass < 10f)
                continue;

            EntManager.TryGetComponent<IFFComponent>(grid.Owner, out var iff);

            // Hide it entirely.
            if (iff != null && (iff.Flags & IFFFlags.Hide) != 0x0)
                continue;

            DrawDocks(handle, grid.Owner, parameters);
        }
    }

    private void DrawDocks(DrawingHandleScreen handle, EntityUid uid, Parameters parameters)
    {
        if (!ShowDocks)
            return;

        const float DockScale = 1f;
        var nent = EntManager.GetNetEntity(uid);

        if (_docks.TryGetValue(nent, out var docks))
        {
            foreach (var state in docks)
            {
                var position = state.Coordinates.Position;
                var uiPosition = parameters.DrawMatrix.Transform(position);

                if (uiPosition.Length() > (WorldRange * 2f) - DockScale)
                    continue;

                var color = Color.ToSrgb(Color.Magenta);

                uiPosition.Y = -uiPosition.Y;

                var verts = new[]
                {
                    parameters.DrawMatrix.Transform(position + new Vector2(-DockScale, -DockScale)),
                    parameters.DrawMatrix.Transform(position + new Vector2(DockScale, -DockScale)),
                    parameters.DrawMatrix.Transform(position + new Vector2(DockScale, DockScale)),
                    parameters.DrawMatrix.Transform(position + new Vector2(-DockScale, DockScale)),
                };

                for (var i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    vert.Y = -vert.Y;
                    verts[i] = ScalePosition(vert);
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color.WithAlpha(0.8f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
            }
        }
    }
}
