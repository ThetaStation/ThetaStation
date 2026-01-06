namespace Content.Shared.Theta.RadarHUD;

public sealed class RadarHUDSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RadarHUDComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<RadarHUDComponent, ComponentRemove>(OnComponentRemoved);
    }

    private void OnComponentStartup(EntityUid uid, RadarHUDComponent component, ComponentStartup args)
    {
        var ev = new RadarHudComponentAdded
        {
            Uid = uid
        };

        RaiseLocalEvent(uid, ref ev);
    }

    private void OnComponentRemoved(EntityUid uid, RadarHUDComponent component, ComponentRemove args)
    {
        var ev = new RadarHudComponentRemoved
        {
            Uid = uid
        };

        RaiseLocalEvent(uid, ref ev);
    }
}

[ByRefEvent]
public struct RadarHudComponentAdded
{
    public EntityUid Uid;
}

[ByRefEvent]
public struct RadarHudComponentRemoved
{
    public EntityUid Uid;
}
