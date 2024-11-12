using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Speech.Muting;
using Robust.Shared.Physics.Systems;
using Content.Shared._CorvaxNext.Resomi;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Content.Shared._CorvaxNext.Resomi.Abilities;
using Content.Shared.Damage.Components;

namespace Content.Server._CorvaxNext.Resomi.Abilities;

public sealed class ResomiSkillSystem : SharedResomiSkillSystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StaminaSystem _Stamina = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResomiSkillComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ResomiSkillComponent, ResomiSwitchAgillityActionEvent>(SwitchAgility);
        SubscribeLocalEvent<ResomiSkillComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnComponentInit(EntityUid uid, ResomiSkillComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.SwitchAgilityActionEntity, component.SwitchAgilityAction, uid);
    }

    /// <summary>
    /// Creates an invisible wall in a free space after some checks.
    /// </summary>
    private void SwitchAgility(EntityUid uid, ResomiSkillComponent component, ResomiSwitchAgillityActionEvent args)
    {
        _Stamina.TryTakeStamina(uid, component.StaminaDamage);

        if (!component.Active)
            OnAgility(uid, component);
        else OffAgility(uid, component);
    }
    private void OnAgility(EntityUid uid, ResomiSkillComponent component)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var comp))
            return;

        _popup.PopupEntity("TEST ON", uid);

        component.SprintSpeedModifier += 0.4f; //+40% (Test)
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        component.Active = !component.Active;
    }
    private void OffAgility(EntityUid uid, ResomiSkillComponent component)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var comp))
            return;

        _popup.PopupEntity("TEST OFF", uid);

        component.SprintSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        component.Active = !component.Active;
    }

    private void OnRefreshMovespeed(EntityUid uid, ResomiSkillComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(1f, component.SprintSpeedModifier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ResomiSkillComponent>();
        while (query.MoveNext(out var uid, out var resomiComp))
        {
            if (!TryComp<StaminaComponent>(uid, out var stamina))
                continue;
            resomiComp.Test = Timing.CurTime;

            if (!resomiComp.Active)
                continue;

            if (Timing.CurTime < resomiComp.NextUpdateTime)
                continue;

            resomiComp.NextUpdateTime = Timing.CurTime + resomiComp.UpdateRate;

            _popup.PopupEntity("TEST UPDATE", uid);

            _Stamina.TryTakeStamina(uid, resomiComp.StaminaDamage);
            if (stamina.StaminaDamage > stamina.CritThreshold * 0.75f)
                OffAgility(uid, resomiComp);
        }
    }
}
