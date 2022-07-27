namespace Content.Shared.Gravity
{
    public abstract class SharedGravitySystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<GridInitializeEvent>(HandleGridInitialize);
            SubscribeLocalEvent<AlertsComponent, EntParentChangedMessage>(OnAlertsParentChange);
            SubscribeLocalEvent<GravityChangedEvent>(OnGravityChange);
            SubscribeLocalEvent<GravityComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<GravityComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, GravityComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not GravityComponentState state) return;

            if (component.EnabledVV == state.Enabled) return;
            component.EnabledVV = state.Enabled;
            RaiseLocalEvent(new GravityChangedEvent(uid, component.EnabledVV));
        }

        private void OnGetState(EntityUid uid, GravityComponent component, ref ComponentGetState args)
        {
            args.State = new GravityComponentState(component.EnabledVV);
        }

        private void OnGravityChange(GravityChangedEvent ev)
        {
            foreach (var (comp, xform) in EntityQuery<AlertsComponent, TransformComponent>(true))
            {
                if (xform.GridUid != ev.ChangedGridIndex) continue;

                if (!ev.HasGravity)
                {
                    _alerts.ShowAlert(comp.Owner, AlertType.Weightless);
                }
                else
                {
                    _alerts.ClearAlert(comp.Owner, AlertType.Weightless);
                }
            }
        }

        private void OnAlertsParentChange(EntityUid uid, AlertsComponent component, ref EntParentChangedMessage args)
        {
            if (IsWeightless(component.Owner))
            {
                _alerts.ShowAlert(uid, AlertType.Weightless);
            }
            else
            {
                _alerts.ClearAlert(uid, AlertType.Weightless);
            }
        }

        private void HandleGridInitialize(GridInitializeEvent ev)
        {
            EntityManager.EnsureComponent<GravityComponent>(ev.EntityUid);
        }

        [Serializable, NetSerializable]
        private sealed class GravityComponentState : ComponentState
        {
            public bool Enabled { get; }

            public GravityComponentState(bool enabled)
            {
                Enabled = enabled;
            }
        }
    }
}
