using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Theta.ShipEvent;

public abstract class SharedCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<RequestCannonShootEvent>(OnShootRequest);
        SubscribeAllEvent<RequestStopCannonShootEvent>(OnStopShootRequest);
    }

    private void OnShootRequest(RequestCannonShootEvent ev)
    {
        // Я не могу сделать ебучий предикшен он работает как говно В ДАННОМ СЛУЧАЕ и я хз как это пофиксить
        // Если не смогу, то будет онли лагающий неткод.
        // На клиенте, какого-то хуя, GunComponent.NextFire У МОЕЙ ХУЙНИ корректно обновляется после AttemptShoot здесь
        // НО КОГДА ТЫ ПЫТАЕШЬСЯ ОПЯТЬ СТРЕЛЬНУТЬ NextFire у компоненты ПОЧЕМУ-ТО ВСЕ ЕЩЕ СТАРЫЙ, БУДТО БЫ AttemptShoot НИКОГДА И НЕ ВЫЗЫВАЕЛСЯ И ТАК НЕСКОЛЬКО ИТЕРАЦИЙ ПОДРЯД
        // ИЗ-ЗА ЭТОЙ ХУЙНИ НА КЛИЕНТЕ ТУРЕЛЬ ПЫТАЕТСЯ ВЫСТРЕЛЬНУТЬ СТОЛЬКО РАЗ СКОЛЬКО ПРОКАЕТ UPDATE КЛИЕНТА И ПОКА ОНО НЕ ОБНОВИТСЯ КАК-ТО.
        // С ХУЯ ЛИ Я ХЗ. ТОТ ЖЕ САМЫЙ НАХУЙ КОД ЛИТТЕРАЛИ ПОЧТИ ПОЛНАЯ КОПИПАСТА У GunSystem РАБОТАЕТ КОРРЕКТНО А У МЕНЯ НЕ РАБОАТЕТ ПОЧЕМУ????????????
        // Ето присто пиздец нахуй я заебался помогите убить кодеров этоу йхуйни с предикшеном я ебал просто рот сука рот ебал нахуй РОТ ЕБАЛ
        //var netManager = IoCManager.Resolve<INetManager>();
       //if(netManager.IsClient)
       //    return;
        var gun = _gunSystem.GetGun(ev.Cannon);
        if (gun == null || !_gunSystem.CanShoot(gun))
            return;

        var coords = EntityCoordinates.FromMap(ev.Cannon, new MapCoordinates(ev.Coordinates, Transform(ev.Cannon).MapID));
        _gunSystem.AttemptShoot(ev.Cannon, gun, coords);

    }

    private void OnStopShootRequest(RequestStopCannonShootEvent ev)
    {
        var gun = _gunSystem.GetGun(ev.Cannon);
        if (gun == null || gun.ShotCounter == 0)
            return;

        gun.ShotCounter = 0;
        gun.ShootCoordinates = null;
        Dirty(gun);
    }
}

[Serializable, NetSerializable]
public sealed class RotateCannonsEvent : EntityEventArgs
{
    public readonly Vector2 Coordinates;

    public readonly List<EntityUid> Cannons;

    public RotateCannonsEvent(Vector2 coordinates, List<EntityUid> cannons)
    {
        Coordinates = coordinates;
        Cannons = cannons;
    }
}

/// <summary>
/// Raised on the client to indicate it'd like to shoot.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestCannonShootEvent : EntityEventArgs
{
    public EntityUid Cannon;
    public Vector2 Coordinates;
}

/// <summary>
/// Raised on the client to request it would like to stop shooting.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestStopCannonShootEvent : EntityEventArgs
{
    public EntityUid Cannon;
}

