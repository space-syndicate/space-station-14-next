using Content.Shared._CorvaxNext.RangeFinder;
using Content.Shared._CorvaxNext.DynamicRange;
using Content.Shared.Pinpointer;
using System.Numerics;
using Robust.Shared.Physics.Systems;

namespace Content.Server._CorvaxNext.RangeFinder;

public sealed class RangeFinderSystem : SharedRangeFinderSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<DynamicRangeComponent> _dynamicRangeQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _dynamicRangeQuery = GetEntityQuery<DynamicRangeComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Обновляем направление для всех RangeFinder'ов
        var query = EntityQueryEnumerator<RangeFinderComponent>();
        while (query.MoveNext(out var uid, out var rangeFinder))
        {
            UpdateDirectionToTarget(uid, rangeFinder);
        }
    }

    public override bool ToggleRangeFinder(EntityUid uid, RangeFinderComponent? rangeFinder = null)
    {
        if (!Resolve(uid, ref rangeFinder))
            return false;

        var isActive = !rangeFinder.IsActive;
        SetActive(uid, isActive, rangeFinder);
        
        // Обновляем визуализацию при переключении активности
        UpdateAppearance(uid, rangeFinder);
        
        return isActive;
    }

    public override void SetActive(EntityUid uid, bool isActive, RangeFinderComponent? rangeFinder = null)
    {
        if (!Resolve(uid, ref rangeFinder))
            return;
        
        if (isActive == rangeFinder.IsActive)
            return;

        rangeFinder.IsActive = isActive;
        
        // При деактивации сбрасываем дистанцию
        if (!isActive)
        {
            rangeFinder.DistanceToTarget = Distance.Unknown;
        }
        
        UpdateAppearance(uid, rangeFinder);
        Dirty(uid, rangeFinder);
    }

    protected override void UpdateDirectionToTarget(EntityUid uid, RangeFinderComponent? rangeFinder = null)
    {
        if (!Resolve(uid, ref rangeFinder))
            return;

        // Если компонент неактивен, убеждаемся что визуализация тоже отключена
        if (!rangeFinder.IsActive)
        {
            if (rangeFinder.DistanceToTarget != Distance.Unknown)
            {
                rangeFinder.DistanceToTarget = Distance.Unknown;
                UpdateAppearance(uid, rangeFinder);
                Dirty(uid, rangeFinder);
            }
            return;
        }

        if (rangeFinder.TargetRange == null || !EntityManager.EntityExists(rangeFinder.TargetRange.Value))
        {
            // Если цель не установлена или недействительна, находим ближайший DynamicRange
            var closestDynamicRange = FindClosestDynamicRange(uid);
            if (closestDynamicRange == null)
            {
                SetDistance(uid, Distance.Unknown, rangeFinder);
                UpdateAppearance(uid, rangeFinder);
                return;
            }
            
            rangeFinder.TargetRange = closestDynamicRange;
        }

        var dirVec = CalculateDirection(uid, rangeFinder.TargetRange.Value);
        var oldDist = rangeFinder.DistanceToTarget;
        
        if (dirVec != null)
        {
            var angle = dirVec.Value.ToWorldAngle();
            TrySetArrowAngle(uid, angle, rangeFinder);
            var dist = CalculateDistance(dirVec.Value, rangeFinder);
            SetDistance(uid, dist, rangeFinder);
        }
        else
        {
            SetDistance(uid, Distance.Unknown, rangeFinder);
        }
        
        if (oldDist != rangeFinder.DistanceToTarget)
            UpdateAppearance(uid, rangeFinder);
    }

    private EntityUid? FindClosestDynamicRange(EntityUid uid)
    {
        if (!_transformQuery.TryGetComponent(uid, out var transform))
            return null;

        var mapId = transform.MapID;
        var worldPos = _transform.GetWorldPosition(transform);

        var closestDistance = float.MaxValue;
        EntityUid? closestEntity = null;

        // Находим ближайший DynamicRange
        var dynamicRangeQuery = EntityQueryEnumerator<DynamicRangeComponent, TransformComponent>();
        while (dynamicRangeQuery.MoveNext(out var rangeUid, out _, out var rangeTransform))
        {
            if (rangeTransform.MapID != mapId)
                continue;

            var distance = (_transform.GetWorldPosition(rangeTransform) - worldPos).LengthSquared();
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEntity = rangeUid;
            }
        }

        return closestEntity;
    }

    private Vector2? CalculateDirection(EntityUid pinUid, EntityUid trgUid)
    {
        if (!_transformQuery.TryGetComponent(pinUid, out var pin))
            return null;
        
        if (!_transformQuery.TryGetComponent(trgUid, out var trg))
            return null;
        
        if (!_dynamicRangeQuery.TryGetComponent(trgUid, out var dynamicRange))
            return null;

        // Проверяем, находятся ли они на одной карте
        if (pin.MapID != trg.MapID)
            return null;

        // Получаем мировую позицию для обоих объектов
        var pinPos = _transform.GetWorldPosition(pin);
        var targetPos = _transform.GetWorldPosition(trg);
        
        // Направление - от позиции RangeFinder к центру DynamicRange
        var centerPos = targetPos + dynamicRange.Origin;
        var dir = centerPos - pinPos;
        
        return dir;
    }

    private Distance CalculateDistance(Vector2 vec, RangeFinderComponent rangeFinder)
    {
        var dist = vec.Length();
        
        // Улучшенная логика определения дистанции, чтобы избежать искажений вблизи цели
        if (dist <= rangeFinder.ReachedDistance / 2) // Добавляем дополнительную проверку
            return Distance.Reached;
        else if (dist <= rangeFinder.ReachedDistance)
            return Distance.Reached;
        else if (dist <= rangeFinder.CloseDistance)
            return Distance.Close;
        else if (dist <= rangeFinder.MediumDistance)
            return Distance.Medium;
        else
            return Distance.Far;
    }

    private void UpdateAppearance(EntityUid uid, RangeFinderComponent rangeFinder, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
            return;

        _appearance.SetData(uid, PinpointerVisuals.IsActive, rangeFinder.IsActive, appearance);
        _appearance.SetData(uid, PinpointerVisuals.TargetDistance, rangeFinder.DistanceToTarget, appearance);
    }
}
