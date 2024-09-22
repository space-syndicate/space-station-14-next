using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Server.Traits.Assorted
{
    public sealed class HeightSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<HeightComponent, ComponentStartup>(InitComponent);
        }

        private void InitComponent(EntityUid uid, HeightComponent component, ComponentStartup args)
        {
            if (TryComp<HeightTraitBlacklistComponent>(uid, out var comp))
                return;

            if (component.Short)
            {
                Scale(uid, component.ShortScale);
            }
            else if (component.Tall)
            {
                Scale(uid, component.TallScale);
            }


        }
        private void Scale(EntityUid uid, float scale)
        {
            var physics = _entityManager.System<SharedPhysicsSystem>();
            var appearance = _entityManager.System<AppearanceSystem>();

            _entityManager.EnsureComponent<ScaleVisualsComponent>(uid);

            var appearanceComponent = _entityManager.EnsureComponent<AppearanceComponent>(uid);
            if (!appearance.TryGetData<Vector2>(uid, ScaleVisuals.Scale, out var oldScale, appearanceComponent))
                oldScale = Vector2.One;

            appearance.SetData(uid, ScaleVisuals.Scale, oldScale * scale, appearanceComponent);

            if (_entityManager.TryGetComponent(uid, out FixturesComponent? manager))
            {
                foreach (var (id, fixture) in manager.Fixtures)
                {
                    switch (fixture.Shape)
                    {
                        case PhysShapeCircle circle:
                            physics.SetPositionRadius(uid, id, fixture, circle, circle.Position * scale, circle.Radius * scale, manager);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }
}
