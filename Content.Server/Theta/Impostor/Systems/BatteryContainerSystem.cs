using Content.Server.Construction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Theta.Impostor.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Wires;
using Robust.Shared.Containers;

namespace Content.Server.Theta.Impostor.Systems;

/// <summary>
/// Makes entity's battery component dependent on external powercell item
/// </summary>
public sealed class BatteryContainerSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _slotSys = default!;
    [Dependency] private BatterySystem _batSys = default!;
    private const string BatterySlotId = "batterySlot";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BatteryContainerComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<BatteryContainerComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<BatteryContainerComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<BatteryContainerComponent, InteractUsingEvent>(OnItemUse, before: new []{typeof(ItemSlotsSystem)}, 
            after: new []{typeof(ConstructionSystem)});
        SubscribeLocalEvent<BatteryContainerComponent, InteractHandEvent>(OnHandUse);
    }

    private void OnCompInit(EntityUid uid, BatteryContainerComponent container, ComponentInit args)
    {
        _slotSys.AddItemSlot(uid, BatterySlotId, container.BatterySlot);
        
        if (container.BatterySlot.HasItem)
        {
            OnEntInserted(uid, container, new EntInsertedIntoContainerMessage(container.BatterySlot.Item!.Value, 
                default!, default!));
        }
    }

    private void OnEntInserted(EntityUid uid, BatteryContainerComponent container, EntInsertedIntoContainerMessage args)
    {
        if (TryComp(uid, out BatteryComponent? oldBat) && TryComp(args.Entity, out BatteryComponent? newBat))
        {
            CopyBatteryData(uid, oldBat, newBat);
        }
    }
    
    private void OnEntRemoved(EntityUid uid, BatteryContainerComponent container, EntRemovedFromContainerMessage args)
    {
        if (TryComp(uid, out BatteryComponent? oldBat) && TryComp(args.Entity, out BatteryComponent? newBat))
        {
            CopyBatteryData(args.Entity, newBat, oldBat);
            ClearBatteryData(uid, oldBat);
        }
    }

    private void CopyBatteryData(EntityUid uid, BatteryComponent to, BatteryComponent from)
    {
        _batSys.MakeRechargeable(uid, true, to); //since unrechargeable batteries don't allow to change their charge to any value
        _batSys.SetMaxCharge(uid, from.MaxCharge, to);
        _batSys.SetCharge(uid, from.Charge, to);
        _batSys.MakeRechargeable(uid, from.IsRechargeable, to);
    }

    private void ClearBatteryData(EntityUid uid, BatteryComponent bat)
    {
        _batSys.MakeRechargeable(uid, true, bat);
        _batSys.SetMaxCharge(uid, 1, bat); //to avoid NaN in charge
        _batSys.SetCharge(uid, 0, bat);
        _batSys.MakeRechargeable(uid, false, bat);
    }

    private void OnItemUse(EntityUid uid, BatteryContainerComponent container, InteractUsingEvent args)
    {
        if (args.Handled)
            return;
        
        if (container.CheckWirePanel && (!TryComp(uid, out WiresPanelComponent? panel) || !panel.Open))
            args.Handled = true;
    }

    private void OnHandUse(EntityUid uid, BatteryContainerComponent container, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (container.CheckWirePanel && (!TryComp(uid, out WiresPanelComponent? panel) || !panel.Open))
            return;
        
        if (container.BatterySlot.HasItem)
            _slotSys.TryEject(uid, container.BatterySlot, args.User, out _);
    }
}
