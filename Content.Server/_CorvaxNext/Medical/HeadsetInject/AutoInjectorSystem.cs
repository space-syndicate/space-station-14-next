using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Inventory;
using Content.Shared.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Robust.Shared.GameObjects;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;

namespace Content.Server.Medical.CritInject;

public sealed class AutoInjectOnCritSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HypospraySystem _hypospray = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AutoInjectOnCritComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        var target = args.Target;

        if (!_inventory.TryGetSlotEntity(target, "ears", out var headset))
            return;

        if (!HasComp<AutoInjectOnCritComponent>(headset))
            return;

        if (!TryComp<HyposprayComponent>(headset.Value, out var hypo))
            return;

        _hypospray.TryDoInject(new Entity<HyposprayComponent>(headset.Value, hypo), target, target);
    }
        private void OnAfterInteract(EntityUid uid, AutoInjectOnCritComponent comp, ref AfterInteractEvent args)
{
    args.Handled = true;
}
}
