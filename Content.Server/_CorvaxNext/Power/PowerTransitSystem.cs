using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DeviceLinking.Events;

namespace Content.Server._CorvaxNext.Power;

/// <summary>
/// This handles...
/// </summary>
public sealed class PowerTransitSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        UpdatesAfter.Add(typeof(PowerNetSystem));

        SubscribeLocalEvent<PowerTransitComponent, NewLinkEvent>(OnNewLink);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<PowerTransitComponent, BatteryComponent>();

        while (query.MoveNext(out var uid, out var transit, out var _))
        {
            TransitPowerWithLinked((uid, transit));
        }
    }

    private void OnNewLink(Entity<PowerTransitComponent> ent, ref NewLinkEvent args)

    {
        if (!TryComp<PowerTransitComponent>(args.Sink, out var comp))
            return;

        LinkPair(ent, (args.Sink, comp));
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

        var anotherEnt = ent.Comp.LinkedPair.GetValueOrDefault();

        if (!TryComp<BatteryComponent>(ent, out var batteryComponent))
            return;

        if (!TryComp<BatteryComponent>(anotherEnt, out var anotherBatteryComponent))
            return;

        var difference = batteryComponent.CurrentCharge - anotherBatteryComponent.CurrentCharge;

        if  (difference == 0)
            return;

        _battery.UseCharge(ent.Owner, difference / 2);
        _battery.AddCharge(anotherEnt.Owner, difference / 2);
    }
}
