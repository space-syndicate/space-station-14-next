using Content.Server.Atmos.EntitySystems;
using Content.Server.Light.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Smoking;
using Content.Shared.IgnitionSource.Components;
using Content.Shared.Smoking.Systems;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    public sealed class MatchstickSystem : SharedMatchstickSystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedPointLightSystem _lights = default!;
        [Dependency] private readonly TransformSystem _transformSystem = default!;

        private readonly HashSet<Entity<MatchstickComponent>> _litMatches = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MatchstickComponent, IsHotEvent>(OnIsHotEvent);
            SubscribeLocalEvent<MatchstickComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnShutdown(Entity<MatchstickComponent> ent, ref ComponentShutdown args)
        {
            _litMatches.Remove(ent);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var match in _litMatches)
            {
                if (match.Comp.CurrentState != SmokableState.Lit || Paused(match) || match.Comp.Deleted)
                    continue;

                if (Transform(match).GridUid is not { } gridUid)
                    return;

                var position = _transformSystem.GetGridOrMapTilePosition(match, Transform(match));
                _atmosphereSystem.HotspotExpose(gridUid, position, 400, 50, match, true);
            }
        }

        private void OnInteractUsing(Entity<MatchstickComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled || ent.Comp.CurrentState != SmokableState.Unlit)
                return;

            var isHotEvent = new IsHotEvent();
            RaiseLocalEvent(args.Used, isHotEvent);

            if (!isHotEvent.IsHot)
                return;

            Ignite(ent, args.User);
            args.Handled = true;
        }

        private void OnIsHotEvent(EntityUid uid, MatchstickComponent component, IsHotEvent args)
        {
            args.IsHot = component.CurrentState == SmokableState.Lit;
        }

        public void Ignite(Entity<MatchstickComponent> matchstick, EntityUid user)
        {
            var component = matchstick.Comp;
            _audio.PlayPvs(component.IgniteSound, matchstick, AudioParams.Default.WithVariation(0.125f).WithVolume(-0.125f));
            SetState((matchstick, component), SmokableState.Lit);
            _litMatches.Add(matchstick);
            matchstick.Owner.SpawnTimer(component.Duration * 1000, delegate
            {
                SetState((matchstick, component), SmokableState.Burnt);
                _litMatches.Remove(matchstick);
            });
        }

        public override bool SetState(Entity<MatchstickComponent> ent, SmokableState value)
        {
            var uid = ent.Owner;
            var component = ent.Comp;
            ent.Comp.State = value;
            if (_lights.TryGetLight(uid, out var pointLightComponent))
            {
            }

            if (EntityManager.TryGetComponent(uid, out ItemComponent? item))
            {
                if (component.CurrentState == SmokableState.Lit)
                    _item.SetHeldPrefix(uid, "lit", component: item);
                else
                    _item.SetHeldPrefix(uid, "unlit", component: item);
            }

            if (EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                _appearance.SetData(uid, SmokingVisuals.Smoking, component.CurrentState, appearance);

            return true;
        }
    }
}
