using System.Numerics;
using Content.Server.Administration.Managers;
using Content.Server.DoAfter;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles.Jobs;
using Content.Server.Theta.Impostor.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Theta.Impostor.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Theta.Impostor.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;

namespace Content.Server.Theta.Impostor.Systems;

public sealed class ImpostorMagicRevolverSystem : EntitySystem
{
    [Dependency] private PopupSystem _popupSys = default!;
    [Dependency] private DoAfterSystem _doAfterSys = default!;
    [Dependency] private SharedGunSystem _gunSys = default!;
    [Dependency] private JobSystem _jobSys = default!;
    [Dependency] private IBanManager _banMan = default!;
    [Dependency] private MobStateSystem _mobSys = default!;
    [Dependency] private MindSystem _mindSys = default!;

    private const string MagicProjectileProtoId = "ImpostorMagicBullet";
    private const string RegularProjectileProtoId = "BulletMagnum";
    private const string CaptainJobId = "Captain";
    private const string CommRelayProtId = "ImpostorCommRelay";
    private const uint MagicBanDuration = 60*8; //minutes
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImpostorMagicBulletComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ImpostorMagicBulletComponent, UseInHandEvent>(OnBulletUseInHand);
        SubscribeLocalEvent<ImpostorMagicBulletComponent, ProjectileHitEvent>(OnBulletHit);
        SubscribeLocalEvent<ImpostorMagicRevolverComponent, InteractUsingEvent>(OnRevolverUse);
        SubscribeLocalEvent<ImpostorMagicRevolverComponent, ImpostorMagicBulletLoadedEvent>(OnBulletDoAfterFinish);
        SubscribeLocalEvent<ImpostorMagicRevolverComponent, AttemptShootEvent>(OnRevolverShoot);
    }

    private void OnExamine(EntityUid uid, ImpostorMagicBulletComponent bullet, ExaminedEvent args)
    {
        if (HasComp<ProjectileComponent>(uid))
            return;
        
        if(bullet.Marked)
            args.PushText(Loc.GetString("impostor-magicbullet-marked"));
    }
    
    private void OnBulletUseInHand(EntityUid uid, ImpostorMagicBulletComponent bullet, UseInHandEvent args)
    {
        if (bullet.Marked)
        {
            _popupSys.PopupEntity(Loc.GetString("impostor-magicbullet-unmark"), uid, args.User);
        }
        else
        {
            _popupSys.PopupEntity(Loc.GetString("impostor-magicbullet-mark"), uid, args.User);
        }
        bullet.Marked = !bullet.Marked;
        Comp<CartridgeAmmoComponent>(uid).Prototype = MagicProjectileProtoId + (bullet.Marked ? "Marked" : "");
        args.Handled = true;
    }
    
    //if bullet is marked and target is impostor - perma kill him and spawn comm relay
    //if bullet is not marked and target is not an impostor (just a regular shitter) - jobban him
    //else jobban captain
    private void OnBulletHit(EntityUid uid, ImpostorMagicBulletComponent bullet, ref ProjectileHitEvent args)
    {
        if (!TryComp(args.Target, out MindContainerComponent? mindContainer) || !mindContainer.HasMind)
            return;
        MindComponent mind = Comp<MindComponent>(mindContainer.Mind.Value);
        if (mind.Session == null)
            return;
        
        bool isImpostor = HasComp<ImpostorRoleComponent>(mindContainer.Mind);
        NetUserId userId = mind.Session.UserId;
        
        _mobSys.ChangeMobState(args.Target, MobState.Dead);
        if (bullet.Marked && isImpostor)
        {
            _mindSys.WipeMind(mindContainer.Mind, mind);
            Spawn(CommRelayProtId, new EntityCoordinates(args.Target, Vector2.Zero));
        }
        else if (!bullet.Marked && !isImpostor)
        {
            if (_jobSys.MindTryGetJob(mindContainer.Mind, out JobComponent? job, out _) && job.PrototypeId != null)
            {
                _banMan.CreateRoleBan(userId, null, null, null, null, job.PrototypeId, 
                    MagicBanDuration, NoteSeverity.None, "Executed by captain", DateTimeOffset.Now);
            }
        }
        else
        {
            if (!TryComp(args.Shooter, out MindContainerComponent? capMindContainer) || !capMindContainer.HasMind)
                return;
            MindComponent capMind = Comp<MindComponent>(mindContainer.Mind.Value);
            if (capMind.Session == null)
                return;
            NetUserId capUserId = mind.Session.UserId;
            _banMan.CreateRoleBan(capUserId, null, null, null, null, CaptainJobId, 
                MagicBanDuration, NoteSeverity.None, "Poor judgment", DateTimeOffset.Now);
        }
    }
    
    private void OnRevolverUse(EntityUid uid, ImpostorMagicRevolverComponent revolver, InteractUsingEvent args)
    {
        if (TryComp(args.Used, out ImpostorMagicBulletComponent? bullet))
        {
            string userName = MetaData(args.User).EntityName;
            _popupSys.PopupCoordinates(Loc.GetString("impostor-magicbullet-tryload", ("name", userName)),
                new EntityCoordinates(args.User, Vector2.Zero), PopupType.MediumCaution);
            _doAfterSys.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(3),
                new ImpostorMagicBulletLoadedEvent(), uid, uid, args.Used));
        }
    }
    
    private void OnBulletDoAfterFinish(EntityUid uid, ImpostorMagicRevolverComponent revolver, ImpostorMagicBulletLoadedEvent args)
    {
        if (args.Used == null) 
            return;
        _gunSys.TryRevolverInsert(uid, Comp<RevolverAmmoProviderComponent>(uid), args.Used.Value, args.User);
    }
    
    private void OnRevolverShoot(EntityUid uid, ImpostorMagicRevolverComponent revolver, ref AttemptShootEvent args)
    {
        RevolverAmmoProviderComponent revolverProvider = Comp<RevolverAmmoProviderComponent>(uid);
        EntityUid? cartridgeUid = revolverProvider.AmmoSlots[revolverProvider.CurrentIndex];
        if (cartridgeUid != null && HasComp<ImpostorMagicBulletComponent>(cartridgeUid))
        {
            CartridgeAmmoComponent cartridge = Comp<CartridgeAmmoComponent>(cartridgeUid.Value);
            
            bool userIsCap = false;
            if (TryComp(args.User, out MindContainerComponent? mindContainer) && mindContainer.HasMind)
            {
                if (_jobSys.MindTryGetJob(mindContainer.Mind, out JobComponent? job, out _))
                    userIsCap = job.PrototypeId == CaptainJobId;
            }
            
            if (!userIsCap)
                cartridge.Prototype = RegularProjectileProtoId;
        }
    }
}
