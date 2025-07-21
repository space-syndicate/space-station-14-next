using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server._CorvaxNext.Power;

/// <summary>
/// This handles...
/// </summary>
public sealed class PowerTransitSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(PowerNetSystem));
        SubscribeLocalEvent<PowerTransitComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<PowerTransitComponent, NewLinkEvent>(OnNewLink);
    }

    private void OnNewLink(Entity<PowerTransitComponent> ent, ref NewLinkEvent args)

    {
        if (!TryComp<PowerTransitComponent>(args.Sink, out var comp))
            return;

        LinkPair(ent, (args.Sink, comp));
    }

    private void OnChargeChanged(Entity<PowerTransitComponent> ent, ref ChargeChangedEvent _)
    {
        TransitPowerWithLinked(ent);
    }

    public void LinkPair(Entity<PowerTransitComponent> ent, Entity<PowerTransitComponent> anotherEnt)
    {
        ent.Comp.LinkedPair = anotherEnt;
        anotherEnt.Comp.LinkedPair = ent;
    }

    public void TransitPowerWithLinked(Entity<PowerTransitComponent> ent)
    {
        if (ent.Comp.LinkedPair == null)
            return;

        if (!TryComp<BatteryComponent>(ent, out var batteryComponent))
            return;

        if (!TryComp<BatteryComponent>(ent, out var anotherBatteryComponent))
            return;

        var difference = batteryComponent.CurrentCharge - anotherBatteryComponent.CurrentCharge;

        if  (difference == 0)
            return;

        batteryComponent.CurrentCharge -= difference / 2;
        anotherBatteryComponent.CurrentCharge += difference / 2;
    }
}
