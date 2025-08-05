using Content.Shared.Mobs;
using Content.Shared.Inventory;
using Content.Shared.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server._CorvaxNext.Medical.AutoInjector;

public sealed class AutoInjectorSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly HypospraySystem _hypospray = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<AutoInjectorComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        var target = args.Target;

        if (!_inventory.TryGetSlotEntity(target, "ears", out var headset))
            return;

        if (!HasComp<AutoInjectorComponent>(headset))
            return;

        if (!TryComp<HyposprayComponent>(headset.Value, out var hypo))
            return;

        _hypospray.TryDoInject(new Entity<HyposprayComponent>(headset.Value, hypo), target, target);
    }
    private void OnAfterInteract(EntityUid uid, AutoInjectorComponent comp, ref AfterInteractEvent args)
    {
        args.Handled = true;
    }
}
