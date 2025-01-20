using Content.Server.Theta.Misc.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Theta.Misc.Systems;

public sealed class AddAmbientLightSystem : EntitySystem
{
    [Dependency] private MapSystem _mapSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddAmbientLightComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(EntityUid uid, AddAmbientLightComponent component, ComponentInit args)
    {
        _mapSys.SetAmbientLight(Transform(uid).MapID, component.AmbientLightColor);
    }
}
