using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Standing;

namespace Content.Shared._White.Collision.Knockdown;

public sealed class KnockdownOnCollideSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnockdownOnCollideComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<KnockdownOnCollideComponent, ThrowDoHitEvent>(OnEntityHit);
    }

    private void OnEntityHit(Entity<KnockdownOnCollideComponent> ent, ref ThrowDoHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void OnProjectileHit(Entity<KnockdownOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ApplyEffects(args.Target, ent.Comp);
    }

    private void ApplyEffects(EntityUid target, KnockdownOnCollideComponent component)
    {
        _standing.Down(target, force: true, dropHeldItems: component.DropItems);
    }
}
