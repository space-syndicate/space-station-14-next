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
using Robust.Shared.Configuration;
using Robust.Shared.Utility;
using Content.Server.GameTicking;
using System.Linq;

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
    [Dependency] private readonly GameTicker _gameTicker = default!;

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
        // Find a suitable grid for spawning
        var gridUid = GetSuitableGrid();
        if (gridUid == null)
            return;

        // Try to spawn the crate within the dynamic range if available
        if (TryGetDynamicRangeSpawnCoordinates(ruleUid, gridUid.Value, component, out var coordinates))
        {
            Spawn(component.CratePrototype, coordinates);
            if (!string.IsNullOrWhiteSpace(component.SpawnEffectPrototype))
                Spawn(component.SpawnEffectPrototype, coordinates);
            component.TotalDropped++;
        }
    }

    /// <summary>
    /// Gets a suitable grid for spawning. Prioritizes grids on active stations.
    /// </summary>
    private EntityUid? GetSuitableGrid()
    {
        // First priority: Get the grid from an active battle royale station
        var battleRoyaleQuery = EntityQueryEnumerator<BattleRoyaleRuleComponent, GameRuleComponent>();
        while (battleRoyaleQuery.MoveNext(out var uid, out _, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            // Get all stations
            var stations = _stationSystem.GetStations();
            foreach (var station in stations)
            {
                if (!TryComp<StationDataComponent>(station, out var data) || data.Grids.Count == 0)
                    continue;

                // Try to get the largest grid
                var largestGrid = _stationSystem.GetLargestGrid(data);
                if (largestGrid != null)
                    return largestGrid;

                // If no largest grid found, return any grid from the station
                if (data.Grids.Count > 0)
                    return data.Grids.First();
            }
        }

        // Second priority: Get any station grid
        var allStationGrids = GetAllStationGrids();
        if (allStationGrids.Count > 0)
            return _random.Pick(allStationGrids);

        // Last resort: Try to get any grid
        var gridQuery = EntityQueryEnumerator<MapGridComponent>();
        if (gridQuery.MoveNext(out var gridEntity, out _))
            return gridEntity;

        return null;
    }

    /// <summary>
    /// Attempts to find spawn coordinates within a dynamic range.
    /// </summary>
    private bool TryGetDynamicRangeSpawnCoordinates(EntityUid ruleUid, EntityUid gridUid, SupplyDropComponent component, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;
        
        // Get the dynamic range component if it exists
        EntityUid? dynamicRangeEntity = null;
        DynamicRangeComponent? dynamicRange = null;
        
        // First check if the rule itself has a dynamic range
        if (TryComp<DynamicRangeComponent>(ruleUid, out var ruleRange))
        {
            dynamicRangeEntity = ruleUid;
            dynamicRange = ruleRange;
        }
        // Otherwise look for any dynamic range in the world
        else
        {
            var rangeQuery = EntityQueryEnumerator<DynamicRangeComponent>();
            if (rangeQuery.MoveNext(out var ent, out var range))
            {
                dynamicRangeEntity = ent;
                dynamicRange = range;
            }
        }
        
        // If no dynamic range found, try to spawn anywhere on the grid
        if (dynamicRangeEntity == null || dynamicRange == null)
            return TryGetGridSpawnCoordinates(gridUid, component, out coordinates);
        
        // We have a dynamic range, so try to spawn within it
        if (!TryComp<TransformComponent>(dynamicRangeEntity.Value, out var rangeXform))
            return TryGetGridSpawnCoordinates(gridUid, component, out coordinates);
        
        // Get the grid component
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;
        
        // Calculate the center and radius of the dynamic range
        var rangePosition = _transform.GetWorldPosition(rangeXform);
        var rangeCenter = rangePosition + dynamicRange.Origin;
        var maxRange = dynamicRange.Range * component.DynamicRangeSpawnMargin;
        
        // Get the grid transform to check if grid and range are on the same map
        if (!TryComp<TransformComponent>(gridUid, out var gridXform))
            return false;
        
        if (gridXform.MapID != rangeXform.MapID)
            return TryGetGridSpawnCoordinates(gridUid, component, out coordinates); // Different maps, spawn anywhere on grid
        
        // Get all tiles on the grid that intersect with the dynamic range circle
        var circle = new Circle(rangeCenter, maxRange);
        var tiles = _mapSystem.GetTilesIntersecting(gridUid, grid, circle).ToList();
        
        if (tiles.Count == 0)
            return TryGetGridSpawnCoordinates(gridUid, component, out coordinates); // No intersection, spawn anywhere on grid
        
        // Shuffle the tiles to add randomness
        _random.Shuffle(tiles);
        
        // Try each tile until we find a valid spawn location
        foreach (var tile in tiles)
        {
            if (!IsTileValidForSpawn(gridUid, tile.GridIndices, component, grid))
                continue;
            
            coordinates = _mapSystem.GridTileToLocal(gridUid, grid, tile.GridIndices);
            return true;
        }
        
        // If we couldn't find a valid spawn location within the range, fall back to spawning anywhere on the grid
        return TryGetGridSpawnCoordinates(gridUid, component, out coordinates);
    }

    /// <summary>
    /// Attempts to find spawn coordinates anywhere on a grid.
    /// </summary>
    private bool TryGetGridSpawnCoordinates(EntityUid gridUid, SupplyDropComponent component, out EntityCoordinates coordinates)
    {
        coordinates = EntityCoordinates.Invalid;
        
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;
        
        var aabb = grid.LocalAABB;
        if (aabb.Size.X <= 0 || aabb.Size.Y <= 0)
            return false;
        
        var attempts = component.MaxSpawnAttempts;
        
        for (var i = 0; i < attempts; i++)
        {
            var randomX = _random.Next((int)aabb.Left, (int)aabb.Right);
            var randomY = _random.Next((int)aabb.Bottom, (int)aabb.Top);
            var tile = new Vector2i(randomX, randomY);
            
            if (IsTileValidForSpawn(gridUid, tile, component, grid))
            {
                coordinates = _mapSystem.GridTileToLocal(gridUid, grid, tile);
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Checks if a tile is valid for spawning a supply crate.
    /// </summary>
    private bool IsTileValidForSpawn(EntityUid gridUid, Vector2i tileIndices, SupplyDropComponent component, MapGridComponent grid)
    {
        var mapUid = Transform(gridUid).MapUid;
        if (mapUid == null)
            return false;
        
        // Check if the tile is space
        if (_atmosphere.IsTileSpace(gridUid, mapUid, tileIndices))
            return false;
        
        // Check if the tile is air blocked (walls, etc.)
        if (_atmosphere.IsTileAirBlocked(gridUid, tileIndices, mapGridComp: grid))
            return false;
        
        // Check for impassable entities
        var physQuery = GetEntityQuery<PhysicsComponent>();
        foreach (var ent in _mapSystem.GetAnchoredEntities(gridUid, grid, tileIndices))
        {
            if (!physQuery.TryGetComponent(ent, out var body))
                continue;
            
            if (body.BodyType == BodyType.Static && 
                body.Hard && 
                (body.CollisionLayer & (int)CollisionGroup.Impassable) != 0)
                return false;
        }
        
        // Check for nearby entities that might interfere
        var tileCoords = _mapSystem.GridTileToLocal(gridUid, grid, tileIndices);
        var mapCoords = _transform.ToMapCoordinates(tileCoords);
        var entitiesNearby = _lookup.GetEntitiesInRange(mapCoords, component.SpawnCheckRadius, LookupFlags.Uncontained);
        
        foreach (var entity in entitiesNearby)
        {
            if (entity == gridUid)
                continue;
            
            if (HasComp<FixturesComponent>(entity) || HasComp<SupplyDropComponent>(entity))
                return false;
        }
        
        return true;
    }

    /// <summary>
    /// Gets the number of connected players.
    /// </summary>
    private int GetPlayerCount()
    {
        var count = 0;
        var playerQuery = EntityQueryEnumerator<ActorComponent>();
        while (playerQuery.MoveNext(out _, out _)) { count++; }
        return count;
    }

    /// <summary>
    /// Gets all grid entities from all stations.
    /// </summary>
    private List<EntityUid> GetAllStationGrids()
    {
        var grids = new List<EntityUid>();
        foreach (var station in _stationSystem.GetStations())
        {
            if (TryComp<StationDataComponent>(station, out var data))
                grids.AddRange(data.Grids);
        }
        return grids;
    }
}
