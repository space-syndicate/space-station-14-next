using System.Numerics;
using Content.Shared._CorvaxNext.FromTileCrafter.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Random;


namespace Content.Shared._CorvaxNext.FromTileCrafter.Systems;

public sealed class FromTileCrafterSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefManager = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FromTileCrafterComponent, FromTileCraftDoAfterEvent>(OnFromTileCraftComplete);
        SubscribeLocalEvent<FromTileCrafterComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnFromTileCraftComplete(Entity<FromTileCrafterComponent> ent, ref FromTileCraftDoAfterEvent args)
    {
        var comp = ent.Comp;
        if (args.Handled || args.Cancelled)
            return;

        var gridUid = GetEntity(args.Grid);
        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tileRef = _maps.GetTileRef(gridUid, grid, args.GridTile);
        var coords = _maps.ToCoordinates(tileRef, grid);

        var spread = grid.TileSize * 0.1f;
        var offset = new Vector2(
            _robustRandom.NextFloat() * spread - 0.5f,
            _robustRandom.NextFloat() * spread - 0.5f);

        if (!_netManager.IsServer)
            Spawn(ent.Comp.EntityToSpawn, coords.Offset(offset));
    }

    private void OnAfterInteract(Entity<FromTileCrafterComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target != null)
            return;

        var comp = ent.Comp;

        if (!_mapManager.TryFindGridAt(_transformSystem.ToMapCoordinates(args.ClickLocation), out var gridUid, out var mapGrid))
            return;

        var tileRef = _maps.GetTileRef(gridUid, mapGrid, args.ClickLocation);
        var tileId = _tileDefManager[tileRef.Tile.TypeId].ID;

        if (!comp.AllowedTileIds.Contains(tileId))
            return;

        var coordinates = _maps.GridTileToLocal(gridUid, mapGrid, tileRef.GridIndices);
        if (!_interactionSystem.InRangeUnobstructed(args.User, coordinates, popup: false))
            return;

        var doAfterEvent = new FromTileCraftDoAfterEvent(GetNetEntity(gridUid), tileRef.GridIndices);
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, comp.Delay, doAfterEvent, ent, used: ent)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameTool,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs, out _);
    }
}
