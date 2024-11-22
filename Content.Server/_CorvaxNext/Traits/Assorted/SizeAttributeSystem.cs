using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Server.Next.Traits.Assorted
{
    public sealed class SizeAttributeSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SizeAttributeComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SizeAttributeComponent, ComponentShutdown>(OnComponentShutdown);
        }
        private void OnComponentShutdown(EntityUid uid, SizeAttributeComponent component, ComponentShutdown args)
        {
            if (!TryComp<SizeAttributeWhitelistComponent>(uid, out var whitelist))
                return;

            if (whitelist.Tall && component.Tall)
            {
                Scale(uid, component, 1 / whitelist.TallScale);
            }
            else if (whitelist.Short && component.Short)
            {
                Scale(uid, component, 1 / whitelist.ShortScale);
            }

            RemComp<SizeAttributeWhitelistComponent>(uid);
        }
        private void OnComponentInit(EntityUid uid, SizeAttributeComponent component, ComponentInit args)
        {
            if (!TryComp<SizeAttributeWhitelistComponent>(uid, out var whitelist))
                return;

            if (whitelist.Tall && component.Tall)
            {
                Scale(uid, component, Math.Max(1, whitelist.TallScale));
            }
            else if (whitelist.Short && component.Short)
            {
                Scale(uid, component, Math.Max(Math.Min(1, whitelist.ShortScale), 0));
            }
        }

        private void Scale(EntityUid uid, SizeAttributeComponent component, float scale)
        {
            if (scale <= 0f)
                return;

            EnsureComp<ScaleVisualsComponent>(uid);

            var appearanceComponent = _entityManager.EnsureComponent<AppearanceComponent>(uid);
            if (!_appearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var oldScale, appearanceComponent))
                oldScale = Vector2.One;

            _appearance.SetData(uid, ScaleVisuals.Scale, oldScale * scale, appearanceComponent);

            if (TryComp(uid, out FixturesComponent? manager))
            {
                foreach (var (id, fixture) in manager.Fixtures)
                {
                    if (!fixture.Hard || fixture.Density <= 1f)
                        continue; // This will skip the flammable fixture and any other fixture that is not supposed to contribute to mass

                    switch (fixture.Shape)
                    {
                        case PhysShapeCircle circle:
                            _physics.SetPositionRadius(uid, id, fixture, circle, circle.Position * scale, circle.Radius * scale, manager);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
