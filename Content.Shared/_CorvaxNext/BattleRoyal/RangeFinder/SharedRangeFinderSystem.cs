using Content.Shared.Pinpointer;
using Content.Shared.Interaction;

namespace Content.Shared._CorvaxNext.RangeFinder;

public abstract class SharedRangeFinderSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RangeFinderComponent, ActivateInWorldEvent>(OnActivate);
    }

    private void OnActivate(EntityUid uid, RangeFinderComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        ToggleRangeFinder(uid, component);
        args.Handled = true;
    }

    /// <summary>
    /// Включает/выключает RangeFinder
    /// </summary>
    /// <returns>True если RangeFinder активирован, false если деактивирован</returns>
    public virtual bool ToggleRangeFinder(EntityUid uid, RangeFinderComponent? rangeFinder = null)
    {
        if (!Resolve(uid, ref rangeFinder))
            return false;

        var isActive = !rangeFinder.IsActive;
        SetActive(uid, isActive, rangeFinder);
        return isActive;
    }

    /// <summary>
    /// Установка активности RangeFinder
    /// </summary>
    public virtual void SetActive(EntityUid uid, bool isActive, RangeFinderComponent? rangeFinder = null)
    {
        if (!Resolve(uid, ref rangeFinder))
            return;
        
        if (isActive == rangeFinder.IsActive)
            return;

        rangeFinder.IsActive = isActive;
        Dirty(uid, rangeFinder);
    }

    /// <summary>
    /// Установка дистанции до цели
    /// </summary>
    public void SetDistance(EntityUid uid, Distance distance, RangeFinderComponent? rangeFinder = null)
    {
        if (!Resolve(uid, ref rangeFinder))
            return;

        if (distance == rangeFinder.DistanceToTarget)
            return;

        rangeFinder.DistanceToTarget = distance;
        Dirty(uid, rangeFinder);
    }

    /// <summary>
    /// Попытка установить угол направления стрелки
    /// </summary>
    public bool TrySetArrowAngle(EntityUid uid, Angle arrowAngle, RangeFinderComponent? rangeFinder = null)
    {
        if (!Resolve(uid, ref rangeFinder))
            return false;

        if (rangeFinder.ArrowAngle.EqualsApprox(arrowAngle, rangeFinder.Precision))
            return false;

        rangeFinder.ArrowAngle = arrowAngle;
        Dirty(uid, rangeFinder);

        return true;
    }

    /// <summary>
    /// Обновление направления к цели
    /// </summary>
    protected virtual void UpdateDirectionToTarget(EntityUid uid, RangeFinderComponent? rangeFinder = null)
    {
        // Реализация в конкретных классах
    }
}
