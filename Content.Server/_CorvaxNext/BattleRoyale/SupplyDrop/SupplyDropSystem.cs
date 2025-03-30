using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;
using Robust.Shared.Maths;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Shared._CorvaxNext.BattleRoyale.DynamicRange;
using Content.Server._CorvaxNext.BattleRoyale.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxNext.BattleRoyale.SupplyDrop;

public sealed class SupplyDropSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private EntityUid? _activeSupplyDropComponent = null;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupplyDropComponent, ComponentStartup>(OnSupplyDropCompStartup);
        SubscribeLocalEvent<SupplyDropComponent, ComponentShutdown>(OnSupplyDropCompShutdown);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }

    private void OnSupplyDropCompStartup(EntityUid uid, SupplyDropComponent component, ComponentStartup args)
    {
        _activeSupplyDropComponent = uid;
        var playerCount = GetPlayerCount();
        var initialDrops = Math.Max(1, (int)Math.Ceiling(playerCount / 10.0));
        for (var i = 0; i < initialDrops; i++)
            SpawnSupplyCrate(uid, component);
    }

    private void OnSupplyDropCompShutdown(EntityUid uid, SupplyDropComponent component, ComponentShutdown args)
    {
        if (_activeSupplyDropComponent == uid)
            _activeSupplyDropComponent = null;
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        if (_activeSupplyDropComponent is not { Valid: true } activeUid) return;
        if (!EntityManager.TryGetComponent(activeUid, out SupplyDropComponent? component))
        {
            _activeSupplyDropComponent = null;
            return;
        }

        component.KillCounter++;
        if (component.KillCounter >= component.KillsPerDrop)
        {
            for (var i = 0; i < component.CratesPerDrop; i++)
                SpawnSupplyCrate(activeUid, component);
            component.KillCounter = 0;
        }
    }

    public void SpawnSupplyCrate(EntityUid ruleUid, SupplyDropComponent component)
    {
        if (!TryGetSpawnCoordinates(ruleUid, component, out var coordinates))
            return;

        Spawn(component.CratePrototype, coordinates);
        if (!string.IsNullOrWhiteSpace(component.SpawnEffectPrototype))
            Spawn(component.SpawnEffectPrototype, coordinates);
        component.TotalDropped++;
    }

    private bool TryGetSpawnCoordinates(EntityUid ruleUid, SupplyDropComponent component, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;
        DynamicRangeComponent? dynamicRange = null;
        EntityUid? dynamicRangeEntity = null;

        if (TryComp<DynamicRangeComponent>(ruleUid, out var rangeOnRule))
        {
            dynamicRange = rangeOnRule;
            dynamicRangeEntity = ruleUid;
        }
        else
        {
            var query = EntityQueryEnumerator<DynamicRangeComponent>();
            if (query.MoveNext(out var ent, out var comp))
            {
                dynamicRange = comp;
                dynamicRangeEntity = ent;
            }
        }

        if (dynamicRange != null && dynamicRangeEntity != null)
        {
            if (TryGetTileCoordinatesWithinRange(dynamicRangeEntity.Value, dynamicRange, component, out coordinates))
                return true;
            return TryGetRandomTileCoordinates(component, out coordinates);
        }
        return TryGetRandomTileCoordinates(component, out coordinates);
    }

    private bool TryGetTileCoordinatesWithinRange(EntityUid rangeEntity, DynamicRangeComponent dynamicRange, SupplyDropComponent component, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;
        if (!TryComp<TransformComponent>(rangeEntity, out var rangeTransform)) return false;

        var mapId = rangeTransform.MapID;
        if (mapId == MapId.Nullspace || !_mapManager.MapExists(mapId)) return false;

        var grids = GetStationGridsOnMap(mapId);
        if (grids.Count == 0) return false;

        var worldCenter = _transform.GetWorldPosition(rangeTransform);
        worldCenter += dynamicRange.Origin;

        var maxRange = dynamicRange.Range * component.DynamicRangeSpawnMargin;
        var maxRangeSq = maxRange * maxRange;
        var maxAttempts = component.MaxSpawnAttempts;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var randomGridUid = _random.Pick(grids);
            if (!TryComp<MapGridComponent>(randomGridUid, out var mapGrid)) continue;

            var aabb = mapGrid.LocalAABB;
            if (aabb.Size.X <= 0 || aabb.Size.Y <= 0) continue;
            var randomX = _random.Next((int)aabb.Left, (int)aabb.Right);
            var randomY = _random.Next((int)aabb.Bottom, (int)aabb.Top);
            var tile = new Vector2i(randomX, randomY);

            var tileCoords = _mapSystem.GridTileToLocal(randomGridUid, mapGrid, tile);
            var tileMapCoords = _transform.ToMapCoordinates(tileCoords);

            if (tileMapCoords.MapId != mapId) continue;
            Vector2 tileWorldPos = tileMapCoords.Position;
            Vector2 centerPos = worldCenter;
            Vector2 delta = tileWorldPos - centerPos;

            float lenSq = delta.LengthSquared();
            if (lenSq > maxRangeSq)
                continue;

            if (IsTileValidForSpawn(randomGridUid, tile, component, mapGrid))
            {
                coordinates = tileCoords;
                return true;
            }
        }
        return false;
    }

    private bool TryGetRandomTileCoordinates(SupplyDropComponent component, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;
        var allGrids = GetAllStationGrids();
        if (allGrids.Count == 0) return false;

        var maxAttempts = component.MaxSpawnAttempts;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var randomGridUid = _random.Pick(allGrids);
            if (!TryComp<MapGridComponent>(randomGridUid, out var mapGrid)) continue;

            var aabb = mapGrid.LocalAABB;
            if (aabb.Size.X <= 0 || aabb.Size.Y <= 0) continue;

            var randomX = _random.Next((int)aabb.Left, (int)aabb.Right);
            var randomY = _random.Next((int)aabb.Bottom, (int)aabb.Top);
            var tile = new Vector2i(randomX, randomY);

            if (IsTileValidForSpawn(randomGridUid, tile, component, mapGrid))
            {
                coordinates = _mapSystem.GridTileToLocal(randomGridUid, mapGrid, tile);
                return true;
            }
        }
        return false;
    }

    private bool IsTileValidForSpawn(EntityUid gridUid, Vector2i tileIndices, SupplyDropComponent component, MapGridComponent grid)
    {
        var mapUid = Transform(gridUid).MapUid;
        if (mapUid == null) return false;

        if (_atmosphere.IsTileSpace(gridUid, mapUid, tileIndices)) return false;
        if (_atmosphere.IsTileAirBlocked(gridUid, tileIndices, mapGridComp: grid)) return false;

        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid, grid, tileIndices))
        {
            if (!physQuery.TryGetComponent(ent, out var body)) continue;
            if (body.BodyType == BodyType.Static && body.Hard && (body.CollisionLayer & (int) CollisionGroup.Impassable) != 0)
                return false;
        }

        var tileCoords = _mapSystem.GridTileToLocal(gridUid, grid, tileIndices);
        var mapCoords = _transform.ToMapCoordinates(tileCoords);
        var checkRadius = component.SpawnCheckRadius;
        var entitiesNearby = _lookup.GetEntitiesInRange(mapCoords, checkRadius, LookupFlags.Uncontained);

        foreach (var entity in entitiesNearby)
        {
            if (entity == gridUid) continue;
            if (HasComp<FixturesComponent>(entity) || HasComp<SupplyDropComponent>(entity))
                return false;
        }
        return true;
    }

    private int GetPlayerCount()
    {
        var count = 0;
        var playerQuery = EntityQueryEnumerator<ActorComponent>();
        while (playerQuery.MoveNext(out _, out _)) { count++; }
        return count;
    }

    private List<EntityUid> GetAllStationGrids()
    {
        var grids = new List<EntityUid>();
        foreach (var station in _stationSystem.GetStations())
        {
            if (TryComp<StationDataComponent>(station, out var data)) grids.AddRange(data.Grids);
        }
        return grids;
    }

    private List<EntityUid> GetStationGridsOnMap(MapId mapId)
    {
        var grids = new List<EntityUid>();
        foreach (var station in _stationSystem.GetStations())
        {
            if (!TryComp<StationDataComponent>(station, out var data)) continue;
            foreach (var gridUid in data.Grids)
            {
                if (Transform(gridUid).MapID == mapId) grids.Add(gridUid);
            }
        }
        return grids;
    }
}
