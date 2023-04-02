using Content.Shared.Theta.ShipEvent.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client.Theta.ShipEvent.Visualizers;

[UsedImplicitly]
public sealed class TurretLoaderVisualizer : AppearanceVisualizer
{
    [Obsolete("Subscribe to AppearanceChangeEvent instead.")]
    public override void OnChangeData(AppearanceComponent appearance)
    {
        base.OnChangeData(appearance);

        if (appearance.TryGetData(TurretLoaderVisuals.Loaded, out bool loaded))
        {
            SetSpriteState(appearance, loaded);
        }
    }
    
    public void SetSpriteState(AppearanceComponent appearance, bool loaded)
    {
        var entity = appearance.Owner;

        var entities = IoCManager.Resolve<IEntityManager>();
        if (!entities.TryGetComponent(entity, out SpriteComponent? sprite)) 
            return;

        sprite.LayerSetState(0, loaded ? "loader-loaded" : "loader");
    }
}
