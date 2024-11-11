using Content.Shared.Body.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CorvaxNext.Teleporter;

public sealed class SyndicateTeleporterSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly EntProtoId TeleportEffectPrototype = "TeleportEffect";

    public override void Initialize()
    {
        SubscribeLocalEvent<SyndicateTeleporterComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<SyndicateTeleporterComponent> teleproter, ref UseInHandEvent e)
    {
        if (e.Handled)
            return;

        var transform = Transform(e.User);

        var direction = transform.LocalRotation.ToWorldVec().Normalized();

        List<EntityCoordinates> safeCoordinates = [];

        for (var i = 0; i <= teleproter.Comp.TeleportationRangeLength; i++)
        {
            var offset = (teleproter.Comp.TeleportationRangeStart + i) * direction;

            var coordinates = transform.Coordinates.Offset(offset).SnapToGrid(EntityManager, _map);

            var tile = coordinates.GetTileRef(EntityManager, _map);

            if (tile is not null && _turf.IsTileBlocked(tile.Value, teleproter.Comp.CollisionGroup))
                continue;

            safeCoordinates.Add(coordinates);
        }

        EntityCoordinates resultCoordinates;

        if (safeCoordinates.Count < 1)
        {
            var offset = (teleproter.Comp.TeleportationRangeStart + _random.NextFloat(teleproter.Comp.TeleportationRangeLength)) * direction;

            resultCoordinates = transform.Coordinates.Offset(offset);
        }
        else
            resultCoordinates = _random.Pick(safeCoordinates);

        Spawn(TeleportEffectPrototype, transform.Coordinates);
        Spawn(TeleportEffectPrototype, resultCoordinates);

        _transform.SetCoordinates(e.User, resultCoordinates);

        if (safeCoordinates.Count < 1)
            _body.GibBody(e.User, true);
    }
}
