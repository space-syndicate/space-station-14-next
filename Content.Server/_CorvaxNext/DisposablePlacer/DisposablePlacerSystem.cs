using Content.Shared._CorvaxNext.DisposablePlacer;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxNext.DisposablePlacer;

public sealed partial class DisposablePlacerSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposablePlacerComponent, ComponentInit>(OnMapInit);
        SubscribeLocalEvent<DisposablePlacerComponent, DisposablePlacerSelectMessage>(OnPrototypeSelectedMessage);
        SubscribeLocalEvent<DisposablePlacerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DisposablePlacerComponent, DisposablePlacerSelectDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DisposablePlacerComponent, ExaminedEvent>(OnExamine);
    }

    public void OnPrototypeSelectedMessage(Entity<DisposablePlacerComponent> entity, ref DisposablePlacerSelectMessage args)
    {
        if (entity.Comp.SelectablePrototypes is null)
            return;

        if (!entity.Comp.SelectablePrototypes.Contains(args.ProtoId))
            return;

        entity.Comp.Prototype = args.ProtoId;
    }

    public void OnMapInit(Entity<DisposablePlacerComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.SelectablePrototypes.Count == 0)
            return;

        entity.Comp.Prototype = entity.Comp.SelectablePrototypes[0];
    }

    private void OnAfterInteract(Entity<DisposablePlacerComponent> entity, ref AfterInteractEvent args)
    {
        if (entity.Comp.Prototype is null)
            return;

        if (args.Target == null || !args.CanReach || entity.Comp.IsPlacing)
            return;

        if (entity.Comp.Whitelist is not null && _whitelist.IsWhitelistFail(entity.Comp.Whitelist, args.Target.Value))
            return;

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, entity.Comp.Delay, new DisposablePlacerSelectDoAfterEvent(), entity, target: args.Target, used: entity)
        {
            BreakOnMove = true,
            NeedHand = true
        });

        if (doAfterCancelled)
            return;

        entity.Comp.IsPlacing = true;

        if (entity.Comp.Sound is not null)
            entity.Comp.PlacingSoundUid = _audioSystem.PlayPvs(entity.Comp.Sound, args.User)!.Value.Entity;

        if (entity.Comp.Effect is not null)
            entity.Comp.PlacingEffectUid = Spawn(entity.Comp.Effect, _xform.GetMoverCoordinates(args.Target.Value));
    }

    private void OnExamine(Entity<DisposablePlacerComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_prototypeManager.TryIndex(entity.Comp.Prototype, out var prototype))
            return;

        args.PushMarkup(Loc.GetString("disposable-placer-examine-details", ("mode", prototype.Name)));
    }

    private void OnDoAfter(Entity<DisposablePlacerComponent> entity, ref DisposablePlacerSelectDoAfterEvent args)
    {
        entity.Comp.IsPlacing = false;

        if (args.Cancelled)
        {
            ClearPlacingEffects(entity);
            return;
        }

        if (entity.Comp.Prototype is null)
            return;

        if (args.Args.Target is null)
            return;

        Spawn(entity.Comp.Prototype, _xform.GetMoverCoordinates(args.Args.Target.Value));
        Del(entity);
    }

    private void ClearPlacingEffects(Entity<DisposablePlacerComponent> entity)
    {
        if (entity.Comp.PlacingEffectUid is not null)
            Del(entity.Comp.PlacingEffectUid);

        if (entity.Comp.PlacingSoundUid is not null)
            Del(entity.Comp.PlacingSoundUid);
    }
}
