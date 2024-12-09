using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Maps;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared._CorvaxNext.Resomi;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared._CorvaxNext.Resomi.Abilities;
using Content.Shared.Damage.Components;
using Robust.Shared.Physics;
using Content.Shared._CorvaxNext.Resomi.Abilities.Hearing;

namespace Content.Server._CorvaxNext.Resomi.Abilities;

public sealed class ListenUpSkillSystem : SharedListenUpSkillSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ListenUpSkillComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ListenUpSkillComponent, ListenUpActionEvent>(SwitchAgility);
        SubscribeLocalEvent<ListenUpSkillComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }
    private void OnComponentInit(Entity<ListenUpSkillComponent> ent, ref ComponentInit args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.SwitchAgilityActionEntity, ent.Comp.SwitchAgilityAction, ent.Owner);
    }
}
