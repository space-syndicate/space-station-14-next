using System.Numerics;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._CorvaxNext.BattleRoyale.DynamicRange;
using Content.Server._CorvaxNext.BattleRoyale.Rules.Components; // Note: Namespace typo in original code was BattleRoyal, assuming it should be BattleRoyale like others
using Content.Shared.GameTicking.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
// Added Robust.Shared.Physics dependency from original 'using' block, which was missing in the diff's 'using' block.
using Robust.Shared.Physics; 
using Robust.Shared.Player; // Added missing dependency from original
using Robust.Shared.Timing;

namespace Content.Server._CorvaxNext.BattleRoyale.SupplyDrop;

/// <summary>
/// Система для управления спавном ящиков снабжения в режиме Battle Royale
/// </summary>
public sealed class SupplyDropSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    // Сохраняем ссылку на компонент SupplyDrop для доступа из глобальных обработчиков
    private EntityUid? _activeSupplyDropComponent = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupplyDropComponent, ComponentStartup>(OnSupplyDropCompStartup);

        // Глобальная подписка на событие убийства
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }

    private void OnSupplyDropCompStartup(EntityUid uid, SupplyDropComponent component, ComponentStartup args)
    {
        // Сохраняем ссылку на активный компонент
        _activeSupplyDropComponent = uid;

        // Подсчитываем общее число игроков
        var playerCount = GetPlayerCount();

        // Округляем вверх и гарантируем минимум 1 ящик
        var initialDrops = Math.Max(1, (int)Math.Ceiling(playerCount / 10.0));

        // Спавним начальные ящики
        for (var i = 0; i < initialDrops; i++)
        {
            SpawnSupplyCrate(uid, component);
        }
    }

    // Глобальный обработчик событий убийства
    private void OnKillReported(ref KillReportedEvent ev)
    {
        // Проверяем, есть ли активный компонент SupplyDrop
        // Ensure EntityMananger is available before TryComp in event handlers
        if (_activeSupplyDropComponent == null || !EntityManager.TryGetComponent<SupplyDropComponent>(_activeSupplyDropComponent.Value, out var component))
            return;

        // Увеличиваем счетчик убийств
        component.KillCounter++;

        // Проверяем, нужно ли спавнить новые ящики
        if (component.KillCounter >= component.KillsPerDrop)
        {
            for (var i = 0; i < component.CratesPerDrop; i++)
            {
                SpawnSupplyCrate(_activeSupplyDropComponent.Value, component);
            }

            // Сбрасываем счетчик убийств
            component.KillCounter = 0;
        }
    }

    /// <summary>
    /// Спавнит ящик снабжения в случайном месте
    /// </summary>
    public void SpawnSupplyCrate(EntityUid ruleUid, SupplyDropComponent component)
    {
        // Получаем координаты для спавна
        if (!TryGetSpawnCoordinates(ruleUid, out var coordinates))
            return;

        // Спавним ящик
        var crate = Spawn(component.CratePrototype, coordinates);

        // Спавним эффект, если он настроен
        if (component.SpawnEffectPrototype != null)
        {
            Spawn(component.SpawnEffectPrototype, coordinates);
        }

        // Увеличиваем счетчик спавнутых ящиков
        component.TotalDropped++;
    }

    /// <summary>
    /// Получает координаты для спавна ящика, учитывая DynamicRange если он есть
    /// </summary>
    private bool TryGetSpawnCoordinates(EntityUid ruleUid, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;

        // Проверяем наличие DynamicRange
        var dynamicRangeQuery = EntityQueryEnumerator<DynamicRangeComponent>();
        EntityUid? dynamicRangeEntity = null;
        DynamicRangeComponent? dynamicRange = null;

        while (dynamicRangeQuery.MoveNext(out var ent, out var comp))
        {
            dynamicRangeEntity = ent;
            dynamicRange = comp;
            break;
        }

        // Если нашли DynamicRange, используем его параметры для определения зоны спавна
        if (dynamicRangeEntity != null && dynamicRange != null)
        {
            return TryGetSpawnCoordinatesWithinRange(dynamicRangeEntity.Value, dynamicRange, out coordinates);
        }

        // Если нет DynamicRange, используем стандартный метод спавна на любой станции
        return TryGetRandomSpawnCoordinates(out coordinates);
    }

    /// <summary>
    /// Получает координаты для спавна внутри безопасной зоны DynamicRange
    /// </summary>
    private bool TryGetSpawnCoordinatesWithinRange(EntityUid rangeEntity, DynamicRangeComponent dynamicRange, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;

        // Получаем преобразование для entity с DynamicRange
        var transform = Transform(rangeEntity);
        var mapId = transform.MapID;

        // Проверяем, что карта существует
        if (mapId == MapId.Nullspace)
            return false;

        // Находим все сетки на карте, принадлежащие станциям
        var grids = new List<EntityUid>();

        // Получаем все станции
        var stations = _stationSystem.GetStations();
        foreach (var station in stations)
        {
            if (TryComp<StationDataComponent>(station, out var stationData))
            {
                foreach (var grid in stationData.Grids)
                {
                    if (Transform(grid).MapID == mapId)
                    {
                        grids.Add(grid);
                    }
                }
            }
        }

        if (grids.Count == 0)
            return false;

        // Выбираем случайную сетку
        var randomGrid = _random.Pick(grids);

        // Получаем мировую позицию центра DynamicRange
        var worldCenter = _transform.GetWorldPosition(transform) + dynamicRange.Origin;

        // Максимальное количество попыток найти подходящую точку
        const int maxAttempts = 50;

        for (var i = 0; i < maxAttempts; i++)
        {
            // Генерируем случайный угол и расстояние внутри безопасной зоны
            var angle = _random.NextFloat() * 2 * MathF.PI;
            // Используем 80% диапазона для безопасности
            var distance = _random.NextFloat() * dynamicRange.Range * 0.8f;

            // Вычисляем точку в мировых координатах
            var offset = new Vector2(
                distance * MathF.Cos(angle),
                distance * MathF.Sin(angle)
            );

            var worldPos = worldCenter + offset;

            // Преобразуем мировую координату в координату сетки
            var gridTransform = Transform(randomGrid);
            var worldToLocal = _transform.GetInvWorldMatrix(gridTransform);
            var localPos = Vector2.Transform(worldPos, worldToLocal);

            // Создаем координаты сущности
            coordinates = new EntityCoordinates(randomGrid, localPos);

            // Проверяем, что точка не заблокирована и доступна для спавна
            if (IsSpawnPositionValid(coordinates, randomGrid))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Получает случайные координаты для спавна на любой станции
    /// </summary>
    private bool TryGetRandomSpawnCoordinates(out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;

        // Получаем все станции
        var stations = _stationSystem.GetStations();
        if (stations.Count == 0)
            return false;

        // Выбираем случайную станцию
        var randomStation = _random.Pick(stations);

        // Получаем все сетки этой станции
        if (!TryComp<StationDataComponent>(randomStation, out var stationData))
            return false;

        if (stationData.Grids.Count == 0)
            return false;

        // Выбираем случайную сетку
        var randomGrid = _random.Pick(stationData.Grids);

        // Максимальное количество попыток найти подходящую точку
        const int maxAttempts = 50;

        for (var i = 0; i < maxAttempts; i++)
        {
            // Получаем локальный AABB сетки
            if (!TryComp<MapGridComponent>(randomGrid, out var grid))
                continue;

            var aabb = grid.LocalAABB;

            // Генерируем случайную точку в пределах AABB
            var randomX = _random.Next((int)aabb.Left, (int)aabb.Right);
            var randomY = _random.Next((int)aabb.Bottom, (int)aabb.Top);

            // Создаем координаты сущности
            coordinates = new EntityCoordinates(randomGrid, new Vector2(randomX, randomY));

            // Проверяем, что точка не заблокирована и доступна для спавна
            if (IsSpawnPositionValid(coordinates, randomGrid))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Проверяет, что позиция подходит для спавна (не заблокирована и есть атмосфера)
    /// </summary>
    private bool IsSpawnPositionValid(EntityCoordinates coordinates, EntityUid gridUid)
    {
        // Проверяем, что координаты действительны
        if (!coordinates.IsValid(EntityManager))
            return false;

        // Проверяем, что сетка существует и у неё есть компонент MapGridComponent
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        // Получаем тайл в указанной позиции
        var tile = grid.GetTileRef(coordinates);

        // Проверяем, не находится ли тайл в космосе
        // Need MapUid for IsTileSpace
        if (!TryComp<TransformComponent>(gridUid, out var gridTransform) || gridTransform.MapUid == null)
             return false; // Should not happen if grid exists, but safety first
        if (_atmosphere.IsTileSpace(gridUid, gridTransform.MapUid.Value, tile.GridIndices))
            return false;

        // Проверяем, не заблокирован ли тайл (стена, объект и т.д.) - Requires MapGridComponent
        if (_atmosphere.IsTileAirBlocked(gridUid, tile.GridIndices, mapGridComp: grid))
            return false;

        // Также можно добавить проверку на столкновения, чтобы избежать спавна внутри объектов
        // Convert EntityCoordinates to MapCoordinates for EntityLookupSystem
        var mapCoords = coordinates.ToMap(EntityManager, _transform);
        var collisionMask = (int) CollisionGroup.Impassable; // Example: Check against impassable objects
        var entitiesNearby = _lookup.GetEntitiesInRange(mapCoords, 0.1f); // Small radius around the center point

        foreach (var entity in entitiesNearby)
        {
            // Skip the grid itself if found
            if (entity == gridUid)
                continue;

             // Check if entity has physics and potentially blocks the space
             // Note: Using FixturesComponent check from original code. A better check might involve PhysicsComponent.CanCollide
             if (HasComp<FixturesComponent>(entity)) // Using the original check
             {
                 // More robust check: Check if the entity actually collides at the target location
                 // This would involve more complex physics queries, FixturesComponent check is simpler but less precise.
                 return false;
             }
        }


        // Дополнительная проверка: Убедимся, что мы не спавним прямо на тайле с другим ящиком снабжения
        // (если ящики сами имеют FixturesComponent, предыдущая проверка может это поймать, но явная проверка не помешает)
        foreach (var entity in entitiesNearby)
        {
            if (HasComp<SupplyDropComponent>(entity)) // Check if another supply drop exists nearby (on the same component type basis)
                 return false; // Avoid stacking drops exactly on top of each other
        }


        return true;
    }


    /// <summary>
    /// Получает количество текущих игроков
    /// </summary>
    private int GetPlayerCount()
    {
        // Считаем только игроков с ActorComponent (предполагается, что это активные игроки)
        // Robust PlayerManager might be another way depending on exact requirements
        var count = 0;
        var playerQuery = EntityQueryEnumerator<ActorComponent>(); // ActorComponent might not be the best indicator for *current* players in a round
                                                                   // Consider using GameTicker or PlayerManager for a more accurate count of active session players.
                                                                   // But following the original logic for now.
        while (playerQuery.MoveNext(out _, out _))
        {
            count++;
        }

        return count;
    }
}
