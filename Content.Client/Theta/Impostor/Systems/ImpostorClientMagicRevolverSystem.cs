using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.Interaction;
using Content.Shared.Theta.Impostor.Components;

namespace Content.Client.Theta.Impostor.Systems;

public sealed class ImpostorClientMagicRevolverSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImpostorMagicRevolverComponent, InteractUsingEvent>(OnRevolverUse, before: new[] { typeof(GunSystem) });
    }
    
    //to avoid mispredict when bullet is instantly inserted into revolver on the client, but there's a doafter on server
    private void OnRevolverUse(EntityUid uid, ImpostorMagicRevolverComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        
        if (HasComp<ImpostorMagicBulletComponent>(args.Used))
            args.Handled = true;
    }
}
