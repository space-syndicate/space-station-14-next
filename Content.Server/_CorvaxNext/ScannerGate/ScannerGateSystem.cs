using Content.Server.DeviceLinking.Systems;
using Content.Shared._CorvaxNext.ScannerGate;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._CorvaxNext.ScannerGate
{
    public sealed class ScannerGateSystem : SharedScannerGateSystem
    {
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedPowerReceiverSystem _receiver = default!;
        [Dependency] private readonly DeviceLinkSystem _link = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ScannerGateComponent, StartCollideEvent>(OnCollide);
            SubscribeLocalEvent<ScannerGateComponent, PowerChangedEvent>(OnPowerChanged);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ScannerGateComponent>();

            while (query.MoveNext(out var uid, out var component))
            {
                if (component.Alarming)
                    if (_gameTiming.CurTime > component.NextCheck)
                    {
                        component.Alarming = false;
                        component.Passing = false;

                        UpdateAppearance(uid, component);
                    }
            }
        }

        private void OnCollide(EntityUid uid, ScannerGateComponent component, ref StartCollideEvent args)
        {
            if (!component.Enabled)
                return;

            if (args.OurFixtureId != component.FixtureId)
                return;

            if (_gameTiming.CurTime < component.NextCheck)
                return;

            TryInvokePort(uid, "GateTrigger");

            component.NextCheck = _gameTiming.CurTime + component.CheckInterval;

            component.Alarming = true;

            if (TryComp<AccessReaderComponent>(uid, out var accessReader))
                if (_accessReader.IsAllowed(args.OtherEntity, uid, accessReader))
                {
                    component.Passing = true;

                    PlayPassed(uid, component);
                    return;
                }

            var checkStatus = CheckItem(args.OtherEntity, component.Blacklist, component.GrantIgnoreWhitelist);

            component.Passing = !checkStatus;

            if (checkStatus)
                PlayDenied(uid, component);
            else
                PlayPassed(uid, component);
        }

        private void OnPowerChanged(EntityUid uid, ScannerGateComponent component, ref PowerChangedEvent args)
        {
            component.Enabled = args.Powered;

            UpdateAppearance(uid, component);
        }

        private void UpdateAppearance(EntityUid uid, ScannerGateComponent component)
        {
            var finalState = ScannerGateStatusVisualState.Idle;

            if (!component.Enabled)
                finalState = ScannerGateStatusVisualState.Off;
            else if (component.Alarming)
                if (component.Passing)
                    finalState = ScannerGateStatusVisualState.Passed;
                else
                    finalState = ScannerGateStatusVisualState.Denied;

            _appearanceSystem.SetData(uid, ScannerGateVisualLayers.Status, finalState);
        }

        private void TryInvokePort(EntityUid uid, string port)
        {
            if (HasComp<DeviceLinkSourceComponent>(uid))
                _link.InvokePort(uid, port);
        }

        private void PlayDenied(EntityUid uid, ScannerGateComponent component)
        {
            TryInvokePort(uid, "GateDenied");

            _audio.PlayPvs(component.CheckDeniedSound, uid);
            UpdateAppearance(uid, component);
        }

        private void PlayPassed(EntityUid uid, ScannerGateComponent component)
        {
            TryInvokePort(uid, "GatePassed");

            _audio.PlayPvs(component.CheckPassedSound, uid);
            UpdateAppearance(uid, component);
        }

        private bool CheckItem(EntityUid entUid, EntityWhitelist? blacklist, EntityWhitelist? whitelist)
        {
            if (_entityWhitelist.IsWhitelistPass(blacklist, entUid))
                return true;

            if (_entityWhitelist.IsWhitelistPass(whitelist, entUid))
                return false;

            if (HasComp<ContainerManagerComponent>(entUid))
                foreach (var container in _containerSystem.GetAllContainers(entUid))
                {
                    foreach (var containedItem in container.ContainedEntities)
                    {
                        if (CheckItem(containedItem, blacklist, whitelist))
                            return true;
                    }
                }

            return false;
        }
    }
}
