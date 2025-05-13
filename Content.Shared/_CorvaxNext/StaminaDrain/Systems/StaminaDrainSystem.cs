using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Localization;
using Content.Shared.Popups;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class StaminaDrainSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private readonly HashSet<EntityUid> _notified = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StaminaDrainComponent, ItemToggleComponent>();
        while (query.MoveNext(out var uid, out var drain, out var toggle))
        {
            if (!toggle.Activated)
            {
                if (_container.TryGetContainingContainer(uid, out var containerTmp))
                    _notified.Remove(containerTmp.Owner);
                continue;
            }

            if (!_container.TryGetContainingContainer(uid, out var container))
                continue;

            var wearer = container.Owner;
            if (!EntityManager.HasComponent<StaminaComponent>(wearer))
                continue;

            if (!_notified.Contains(wearer))
            {
                _notified.Add(wearer);
                _popup.PopupClient(Loc.GetString("stamina-drain-popup"), wearer);
            }

            _stamina.TakeStaminaDamage(wearer, drain.StaminaPerSecond * frameTime, visual: false);
        }
    }
}
