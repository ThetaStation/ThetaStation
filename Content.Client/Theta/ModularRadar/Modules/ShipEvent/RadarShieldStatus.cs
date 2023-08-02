using System.Numerics;
using Content.Shared.Theta.ShipEvent.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.Theta.ModularRadar.Modules.ShipEvent;

public sealed class RadarShieldStatus : RadarModule
{
    [Dependency] private readonly IEntityManager entMan = default!; 
    private readonly TransformSystem formSys = default!;
    
    public RadarShieldStatus(ModularRadarControl parentRadar) : base(parentRadar)
    {
        formSys = entMan.System<TransformSystem>();
    }

    public override void Draw(DrawingHandleScreen handle, Parameters parameters)
    {
        base.Draw(handle, parameters);
        foreach ((var form, var shield) in entMan.EntityQuery<TransformComponent, CircularShieldComponent>())
        {
            Vector2 pos = formSys.GetWorldPosition(form);
            pos = parameters.DrawMatrix.Transform(pos);
            pos.Y *= -1;
            pos = ScalePosition(pos);
            
            handle.DrawCircle(pos, 5f, shield.Color);
            if (shield.CanWork)
            {
                handle.DrawCircle(pos, shield.Radius, Color.Yellow, false);
                handle.DrawLine(pos, pos + shield.Angle.ToVec() * shield.Radius, Color.Yellow);
                handle.DrawLine(pos, pos + (shield.Angle - shield.Width / 2).ToVec() * shield.Radius, Color.Red);
                handle.DrawLine(pos, pos + (shield.Angle + shield.Width / 2).ToVec() * shield.Radius, Color.Blue);
            }
        }
    }
}
