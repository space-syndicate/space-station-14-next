using Content.Shared._CorvaxNext.ReplaceOnUseHand;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Server._CorvaxNext.ReplaceOnUseHand;
public sealed partial class ReplaceOnUseHandSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReplaceOnUseHandComponent, InteractHandEvent>(OnInteractHandEvent);
        SubscribeLocalEvent<ReplaceOnUseHandComponent, ReplaceOnUseDoAfterEvent>(OnDoAfter);
    }
    private void OnInteractHandEvent(Entity<ReplaceOnUseHandComponent> entity, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (entity.Comp.ReplacingEntity is null)
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, entity.Comp.Delay, new ReplaceOnUseDoAfterEvent(), entity, target: entity)
        {
            BreakOnMove = true
        });
    }

    private void OnDoAfter(Entity<ReplaceOnUseHandComponent> entity, ref ReplaceOnUseDoAfterEvent args)
    {
        var coords = _xform.GetMoverCoordinates(entity);

        Spawn(entity.Comp.ReplacingEntity, coords);

        if (entity.Comp.ReplaceSound is not null)
            _audioSystem.PlayPvs(entity.Comp.ReplaceSound, coords);

        Del(entity);
    }
}
