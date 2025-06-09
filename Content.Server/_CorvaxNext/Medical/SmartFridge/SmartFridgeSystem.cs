using Content.Server.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Shared._CorvaxNext.Medical.SmartFridge;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Server.Labels;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Verbs;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using System.Linq;
using Content.Shared.FixedPoint;

namespace Content.Server._CorvaxNext.Medical.SmartFridge;

public sealed class SmartFridgeSystem : SharedSmartFridgeSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly AnchorableSystem _anchorable = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SmartFridgeComponent>(SmartFridgeUiKey.Key,
            subs =>
        {
            subs.Event<SmartFridgeEjectMessage>(OnSmartFridgeEjectMessage);
        });

        SubscribeLocalEvent<SmartFridgeComponent, MapInitEvent>(MapInit, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<SmartFridgeComponent, ItemSlotEjectAttemptEvent>(OnItemEjectEvent);
        SubscribeLocalEvent<SmartFridgeComponent, InteractUsingEvent>(OnInteractEvent);
    }

    private void OnInteractEvent(EntityUid entity, SmartFridgeComponent component, ref InteractUsingEvent ev)
    {
        if (_tags.HasTag(ev.Used, "Wrench"))
        {
            _anchorable.TryToggleAnchor(entity, ev.User, ev.Used);
            ev.Handled = true;
        }

        if (!_anchorable.IsPowered(entity, _entityManager))
        {
            ev.Handled = true;
            return;
        }

        if (component.StorageWhitelist != null)
        {
            if (!_tags.HasAnyTag(ev.Used, component.StorageWhitelist.Tags!.ToArray()))
            {
                ev.Handled = true;
                return;
            }
        }

        if (!_itemSlotsSystem.TryInsertEmpty(ev.Target, ev.Used, ev.User, true))
        return;

    ProcessReagentLabeling(ev.Used);
    UpdateFridgeInventory(entity, component);

    ev.Handled = true;
}

    private void ProcessReagentLabeling(EntityUid entity)
{
    if (!_solutionContainerSystem.TryGetDrainableSolution(entity, out _, out var solution) || solution.Volume <= 0)
        return;

    var reagents = solution.Contents;
    var totalQuantity = solution.Volume.Float();

    switch (reagents.Count)
    {
        case 0:
            return;

        case 1:
            LabelSingleReagent(entity, reagents[0], totalQuantity);
            break;

        case 2:
            LabelTwoReagents(entity, reagents[0], reagents[1], totalQuantity);
            break;

        default:
            LabelComplexMixture(entity, reagents, totalQuantity);
            break;
    }
}

private void LabelSingleReagent(EntityUid entity, ReagentQuantity reagent, float totalQuantity)
{
    if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var reagentProto))
        return;

    var purity = 100f * reagent.Quantity.Float() / totalQuantity;
    var roundedPurity = (int)Math.Round(purity);
    var units = (int)Math.Round(totalQuantity);

    _label.Label(entity, purity >= 99.99f
        ? Loc.GetString("smart-fridge-pure-with-units-label",
            ("reagent", reagentProto.LocalizedName),
            ("units", units))
        : Loc.GetString("smart-fridge-impure-label",
            ("reagent", reagentProto.LocalizedName),
            ("purity", roundedPurity),
            ("units", units)));
}

private void LabelTwoReagents(EntityUid entity, ReagentQuantity reagent1, ReagentQuantity reagent2, float totalQuantity)
{
    if (!_prototypeManager.TryIndex<ReagentPrototype>(reagent1.Reagent.Prototype, out var proto1) ||
        !_prototypeManager.TryIndex<ReagentPrototype>(reagent2.Reagent.Prototype, out var proto2))
        return;

    var percent1 = (int)Math.Round(100f * reagent1.Quantity.Float() / totalQuantity);
    var percent2 = (int)Math.Round(100f * reagent2.Quantity.Float() / totalQuantity);
    var units = (int)Math.Round(totalQuantity);

    _label.Label(entity, Loc.GetString("smart-fridge-mixed-label",
        ("reagent1", proto1.LocalizedName),
        ("percent1", percent1),
        ("reagent2", proto2.LocalizedName),
        ("percent2", percent2),
        ("units", units)));
}

private void LabelComplexMixture(EntityUid entity, IReadOnlyList<ReagentQuantity> reagents, float totalQuantity)
{
    var primaryReagents = reagents
        .OrderByDescending(r => r.Quantity.Float())
        .Take(3)
        .ToList();

    if (primaryReagents.Count < 3 ||
        !_prototypeManager.TryIndex<ReagentPrototype>(primaryReagents[0].Reagent.Prototype, out var proto1) ||
        !_prototypeManager.TryIndex<ReagentPrototype>(primaryReagents[1].Reagent.Prototype, out var proto2) ||
        !_prototypeManager.TryIndex<ReagentPrototype>(primaryReagents[2].Reagent.Prototype, out var proto3))
        return;

    var percent1 = (int)Math.Round(100f * primaryReagents[0].Quantity.Float() / totalQuantity);
    var percent2 = (int)Math.Round(100f * primaryReagents[1].Quantity.Float() / totalQuantity);
    var percent3 = (int)Math.Round(100f * primaryReagents[2].Quantity.Float() / totalQuantity);
    var othersPercent = 100 - percent1 - percent2 - percent3;
    var units = (int)Math.Round(totalQuantity);

    string labelText;

    if (reagents.Count > 3 && othersPercent > 0)
    {
        labelText = Loc.GetString("smart-fridge-mixed-multiple-label",
            ("reagent1", proto1.LocalizedName),
            ("percent1", percent1),
            ("reagent2", proto2.LocalizedName),
            ("percent2", percent2),
            ("reagent3", proto3.LocalizedName),
            ("percent3", percent3),
            ("othersPercent", othersPercent),
            ("units", units));
    }
    else
    {
        labelText = Loc.GetString("smart-fridge-triple-label",
            ("reagent1", proto1.LocalizedName),
            ("percent1", percent1),
            ("reagent2", proto2.LocalizedName),
            ("percent2", percent2),
            ("reagent3", proto3.LocalizedName),
            ("percent3", percent3),
            ("units", units));
    }

    _label.Label(entity, labelText);
}

    private void UpdateFridgeInventory(EntityUid uid, SmartFridgeComponent component)
    {
        component.Inventory = GetInventory(uid);
        Dirty(uid, component);
    }

    private void OnItemEjectEvent(EntityUid entity, SmartFridgeComponent component, ref ItemSlotEjectAttemptEvent ev)
    {
        if (component.SlotToEjectFrom == ev.Slot)
        {
            Dirty(entity, component);
            return;
        }

        ev.Cancelled = !component.Ejecting;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SmartFridgeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Ejecting)
                continue;

            comp.EjectAccumulator += frameTime;
            if (!(comp.EjectAccumulator >= comp.EjectDelay))
                continue;

            comp.EjectAccumulator = 0f;
            comp.Ejecting = false;

            EjectItem(uid, comp);
        }
    }

    private void MapInit(EntityUid uid, SmartFridgeComponent component, MapInitEvent _)
    {
        SetupSmartFridge(uid, component);
    }

    private void OnSmartFridgeEjectMessage(EntityUid uid, SmartFridgeComponent component, SmartFridgeEjectMessage args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (args.Actor is not { Valid: true } entity || Deleted(entity))
            return;

        VendFromSlot(uid, args.Id);
        Dirty(uid, component);
    }

    private void VendFromSlot(EntityUid uid, string itemSlotToEject, SmartFridgeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!this.IsPowered(uid, EntityManager))
        {
            return;
        }

        var item = _itemSlotsSystem.GetItemOrNull(uid, itemSlotToEject);

        if (item == null)
            return;

        if (!_itemSlotsSystem.TryGetSlot(uid, itemSlotToEject, out var itemSlot) && itemSlot == null)
            return;

        component.Ejecting = true;
        component.SlotToEjectFrom = itemSlot;

        _audio.PlayPvs(component.SoundVend, uid);
    }

    private void EjectItem(EntityUid uid, SmartFridgeComponent component)
    {
        if (component.SlotToEjectFrom == null ||
            !_itemSlotsSystem.TryEject(uid, component.SlotToEjectFrom, null, out _))
            return;

        component.Inventory = GetInventory(uid);
        component.SlotToEjectFrom = null;

        Dirty(uid, component);
    }

    private void SetupSmartFridge(EntityUid uid, SmartFridgeComponent component)
    {
        for (var i = 0; i < component.NumSlots; i++)
        {
            var storageSlotId = SmartFridgeComponent.BaseStorageSlotId + i;
            ItemSlot storageComponent = new()
            {
                Whitelist = component.StorageWhitelist,
                Swap = false,
                EjectOnBreak = true,
            };

            component.StorageSlotIds.Add(storageSlotId);
            component.StorageSlots.Add(storageComponent);
            component.StorageSlots[i].Name = "Storage Slot " + (i+1);
            _itemSlotsSystem.AddItemSlot(uid, component.StorageSlotIds[i], component.StorageSlots[i]);
        }

        _itemSlotsSystem.AddItemSlot(uid, "itemSlot", component.FridgeSlots);
    }
}
