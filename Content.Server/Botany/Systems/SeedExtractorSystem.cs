using Content.Server.Botany.Components;
using Content.Server.Construction; // Corvax-Next
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server.Botany.Systems;

public sealed class SeedExtractorSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly BotanySystem _botanySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedExtractorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SeedExtractorComponent, RefreshPartsEvent>(OnRefreshParts); // Corvax-Next
        SubscribeLocalEvent<SeedExtractorComponent, UpgradeExamineEvent>(OnUpgradeExamine); // Corvax-Next
    }

    private void OnInteractUsing(EntityUid uid, SeedExtractorComponent seedExtractor, InteractUsingEvent args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (!TryComp(args.Used, out ProduceComponent? produce))
            return;
        if (!_botanySystem.TryGetSeed(produce, out var seed) || seed.Seedless)
        {
            _popupSystem.PopupCursor(Loc.GetString("seed-extractor-component-no-seeds", ("name", args.Used)),
                args.User, PopupType.MediumCaution);
            return;
        }

        _popupSystem.PopupCursor(Loc.GetString("seed-extractor-component-interact-message", ("name", args.Used)),
            args.User, PopupType.Medium);

        QueueDel(args.Used);

        var amount = _random.Next(seedExtractor.BaseMinSeeds, seedExtractor.BaseMaxSeeds + 1);
        var coords = Transform(uid).Coordinates;

        var packetSeed = seed;
        if (amount > 1)
            packetSeed.Unique = false;

        for (var i = 0; i < amount; i++)
        {
            _botanySystem.SpawnSeedPacket(packetSeed, coords, args.User);
        }
    }

    // Corvax-Next
    private void OnRefreshParts(EntityUid uid, SeedExtractorComponent seedExtractor, RefreshPartsEvent args)
    {
        var manipulatorQuality = args.PartRatings["Manipulator"];

        seedExtractor.BaseMaxSeeds = (int)MathF.Pow(new SeedExtractorComponent().BaseMaxSeeds, manipulatorQuality - 1);
    }

    private void OnUpgradeExamine(EntityUid uid, SeedExtractorComponent seedExtractor, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("seed-extractor-component-upgrade-seed-yield", seedExtractor.BaseMaxSeeds);
    }
    // End Corvax-Next
}
