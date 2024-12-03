using System.Diagnostics.CodeAnalysis;
using Content.Shared._CorvaxNext.Footprints.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Movement.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxNext.Footprints;

public sealed class SharedFootprintSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly INetManager _net = default!;

    public static readonly FixedPoint2 MinFootprintVolume = 1;

    public static readonly FixedPoint2 MaxFootprintVolume = 2;

    public static readonly FixedPoint2 MaxFootprintVolumeOnTile = 50;

    public static readonly EntProtoId FootprintPrototypeId = "Footprint";

    public const string FootprintOwnerSolution = "print";

    public const string FootprintSolution = "print";

    public const string PuddleSolution = "puddle";

    public override void Initialize()
    {
        SubscribeLocalEvent<FootprintComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);

        SubscribeLocalEvent<FootprintOwnerComponent, FootstepEvent>(OnFootstep);
    }

    private void OnAnchorStateChanged(Entity<FootprintComponent> entity, ref AnchorStateChangedEvent e)
    {
        //if (!e.Anchored)
        //    QueueDel(entity);
    }

    private void OnFootstep(Entity<FootprintOwnerComponent> entity, ref FootstepEvent e)
    {
        if (_net.IsClient)
            return;

        var transform = Transform(entity);

        if (transform.GridUid is null)
            return;

        if (!TryComp<MapGridComponent>(transform.GridUid.Value, out var gridComponent))
            return;

        var tile = _map.GetTileRef(transform.GridUid.Value, gridComponent, transform.Coordinates);

        if (TryPuddleInteraction(entity, (transform.GridUid.Value, gridComponent), tile))
            return;

        FootprintInteraction(entity, (transform.GridUid.Value, gridComponent), tile);
    }

    private bool TryPuddleInteraction(Entity<FootprintOwnerComponent> entity, Entity<MapGridComponent> grid, TileRef tile)
    {
        if (!TryGetAnchoredEntity<PuddleComponent>(grid, tile.GridIndices, out var puddle))
            return false;

        if (!_solution.EnsureSolutionEntity(entity.Owner, FootprintOwnerSolution, out _, out var solution, FixedPoint2.Max(entity.Comp.MaxFootVolume, entity.Comp.MaxBodyVolume)))
            return false;

        if (!_solution.TryGetSolution(puddle.Value.Owner, PuddleSolution, out var puddleSolution, out _))
            return false;

        _solution.TryTransferSolution(puddleSolution.Value, solution.Value.Comp.Solution, GetFootprintVolume(entity, solution.Value));

        _solution.TryTransferSolution(solution.Value, puddleSolution.Value.Comp.Solution, entity.Comp.MaxFootVolume - solution.Value.Comp.Solution.Volume);

        Dirty(puddle.Value);

        return true;
    }

    private void FootprintInteraction(Entity<FootprintOwnerComponent> entity, Entity<MapGridComponent> grid, TileRef tile)
    {
        if (!_solution.TryGetSolution(entity.Owner, FootprintOwnerSolution, out var solution, out _))
            return;

        var volume = GetFootprintVolume(entity, solution.Value);

        if (volume < MinFootprintVolume)
            return;

        if (!TryGetAnchoredEntity<FootprintComponent>(grid, tile.GridIndices, out var footprint))
        {
            var footprintEntity = SpawnAtPosition(FootprintPrototypeId, Transform(entity.Owner).Coordinates);

            footprint = (footprintEntity, Comp<FootprintComponent>(footprintEntity));
        }

        if (!_solution.EnsureSolutionEntity(footprint.Value.Owner, PuddleSolution, out _, out var footprintSolution, MaxFootprintVolumeOnTile))
            return;

        footprint.Value.Comp.Footprints.Add(new(footprintSolution.Value.Comp.Solution.GetColor(_prototype).WithAlpha((float)volume / (float)MaxFootprintVolume)));

        Dirty(footprint.Value);

        _solution.TryTransferSolution(footprintSolution.Value, solution.Value.Comp.Solution, volume);

        if (!TryGetNetEntity(footprint, out var netFootprint))
            return;

        RaiseNetworkEvent(new FootprintChangedEvent(netFootprint.Value));
    }

    private static FixedPoint2 GetFootprintVolume(Entity<FootprintOwnerComponent> entity, Entity<SolutionComponent> solution)
    {
        return FixedPoint2.Min(solution.Comp.Solution.Volume, (MaxFootprintVolume - MinFootprintVolume) * (solution.Comp.Solution.Volume / entity.Comp.MaxFootVolume) + MinFootprintVolume);
    }

    private bool TryGetAnchoredEntity<T>(Entity<MapGridComponent> grid, Vector2i pos, [NotNullWhen(true)] out Entity<T>? entity) where T : IComponent
    {
        var anchoredEnumerator = _map.GetAnchoredEntitiesEnumerator(grid, grid, pos);
        var entityQuery = GetEntityQuery<T>();

        while (anchoredEnumerator.MoveNext(out var ent))
            if (entityQuery.TryComp(ent, out var comp))
            {
                entity = (ent.Value, comp);
                return true;
            }

        entity = null;
        return false;
    }
}
