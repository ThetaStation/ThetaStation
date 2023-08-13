using Content.Shared.Actions;
using Content.Shared.Gravity;
using Content.Shared.Hands;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Theta.RadarHUD;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedJetpackSystem : EntitySystem
{
    [Dependency] protected readonly MovementSpeedModifierSystem MovementSpeedModifier = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _formSys = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, GetItemActionsEvent>(OnJetpackGetAction);
        SubscribeLocalEvent<JetpackComponent, GotEquippedHandEvent>(OnJetpackPickedUp);
        SubscribeLocalEvent<JetpackComponent, DroppedEvent>(OnJetpackDropped);
        SubscribeLocalEvent<JetpackComponent, ToggleJetpackEvent>(OnJetpackToggle);
        SubscribeLocalEvent<JetpackComponent, CanWeightlessMoveEvent>(OnJetpackCanWeightlessMove);

        SubscribeLocalEvent<JetpackUserComponent, CanWeightlessMoveEvent>(OnJetpackUserCanWeightless);
        SubscribeLocalEvent<JetpackUserComponent, EntParentChangedMessage>(OnJetpackUserEntParentChanged);
        SubscribeLocalEvent<JetpackUserComponent, ComponentGetState>(OnJetpackUserGetState);
        SubscribeLocalEvent<JetpackUserComponent, ComponentHandleState>(OnJetpackUserHandleState);

        SubscribeLocalEvent<GravityChangedEvent>(OnJetpackUserGravityChanged);
    }

    private void OnJetpackCanWeightlessMove(EntityUid uid, JetpackComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnJetpackUserGravityChanged(ref GravityChangedEvent ev)
    {
        var gridUid = ev.ChangedGridIndex;
        var jetpackQuery = GetEntityQuery<JetpackComponent>();

        var query = EntityQueryEnumerator<JetpackUserComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var user, out var transform))
        {
            if (transform.GridUid == gridUid && jetpackQuery.TryGetComponent(user.Jetpack, out var jetpack))
            {
                SetEnabled(jetpack, !ev.HasGravity, uid);
                if(ev.HasGravity)
                    _popup.PopupClient(Loc.GetString("jetpack-to-grid"), uid, uid);
            }
        }
    }

    private void OnJetpackUserHandleState(EntityUid uid, JetpackUserComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not JetpackUserComponentState state) return;
        component.Jetpack = state.Jetpack;
    }

    private void OnJetpackUserGetState(EntityUid uid, JetpackUserComponent component, ref ComponentGetState args)
    {
        args.State = new JetpackUserComponentState()
        {
            Jetpack = component.Jetpack,
        };
    }

    private void OnJetpackPickedUp(EntityUid uid, JetpackComponent component, GotEquippedHandEvent args)
    {
        SetupUser(args.User, component);
    }
    
    private void OnJetpackDropped(EntityUid uid, JetpackComponent component, DroppedEvent args)
    {
        SetEnabled(component, false, args.User);
        RemoveUser(args.User);
    }

    private void OnJetpackUserCanWeightless(EntityUid uid, JetpackUserComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = IsUserFlying(uid);
    }

    private void OnJetpackUserEntParentChanged(EntityUid uid, JetpackUserComponent component, ref EntParentChangedMessage args)
    {
        if (TryComp<JetpackComponent>(component.Jetpack, out var jetpack))
        {
            bool canEnable = CanEnableOnGrid(args.Transform.GridUid);
            SetEnabled(jetpack, canEnable, uid);

            //For some reason mover relay prevents player's mover from updating it's relative entity (jetpack mover has correct entity btw)
            //which leads to undesirable effects, like player's eye thinking it's still attached to the grid you've just left, causing it to rotate with it
            //so yeah, doing this manually
            if (TryComp<InputMoverComponent>(uid, out var umover))
            {
                _mover.TryUpdateRelative(umover, args.Transform);
            }

            if(!canEnable)
                _popup.PopupClient(Loc.GetString("jetpack-to-grid"), uid, uid);
        }
    }

    private void SetupUser(EntityUid uid, JetpackComponent component)
    {
        var user = EnsureComp<JetpackUserComponent>(uid);
        user.Jetpack = component.Owner;
    }

    private void RemoveUser(EntityUid uid)
    {
        if (!RemComp<JetpackUserComponent>(uid)) return;
        RemComp<RelayInputMoverComponent>(uid);
    }

    private void OnJetpackToggle(EntityUid uid, JetpackComponent component, ToggleJetpackEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<TransformComponent>(uid, out var xform) && !CanEnableOnGrid(xform.GridUid))
        {
            _popup.PopupClient(Loc.GetString("jetpack-no-station"), uid, args.Performer);
            return;
        }

        SetEnabled(component, !IsEnabled(uid));
    }

    private bool CanEnableOnGrid(EntityUid? gridUid)
    {
        return gridUid == null || !HasComp<GravityComponent>(gridUid);
    }

    private void OnJetpackGetAction(EntityUid uid, JetpackComponent component, GetItemActionsEvent args)
    {
        args.Actions.Add(component.ToggleAction);
    }

    private bool IsEnabled(EntityUid uid)
    {
        return HasComp<ActiveJetpackComponent>(uid);
    }

    public void SetEnabled(JetpackComponent component, bool enabled, EntityUid? user = null)
    {
        if (IsEnabled(component.Owner) == enabled || enabled && !CanEnable(component)) 
            return;

        if (enabled)
        {
            EnsureComp<ActiveJetpackComponent>(component.Owner);
        }
        else
        {
            RemComp<ActiveJetpackComponent>(component.Owner);
        }

        if (user == null)
        {
            Container.TryGetContainingContainer(component.Owner, out var container);
            user = container?.Owner;
        }

        if (user != null)
        {
            if (enabled)
            {
                _mover.SetRelay(user.Value, component.Owner);
                MovementSpeedModifier.RefreshMovementSpeedModifiers(user.Value);
            }
            else
            {
                RemComp<RelayInputMoverComponent>(user.Value);
            }
        }

        Appearance.SetData(component.Owner, JetpackVisuals.Enabled, enabled);
        Dirty(component);
    }

    public bool IsUserFlying(EntityUid uid)
    {
        if (TryComp(uid, out JetpackUserComponent? user))
            return IsEnabled(user.Jetpack);
        return false;
    }

    protected virtual bool CanEnable(JetpackComponent component)
    {
        return true;
    }

    [Serializable, NetSerializable]
    protected sealed class JetpackUserComponentState : ComponentState
    {
        public EntityUid Jetpack;
    }
}

[Serializable, NetSerializable]
public enum JetpackVisuals : byte
{
    Enabled
}
