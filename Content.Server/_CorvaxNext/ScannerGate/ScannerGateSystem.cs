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

namespace Content.Server._CorvaxNext.ScannerGate;

public sealed class ScannerGateSystem : EntitySystem
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
            if (component.Alarming == false)
                continue;

            if (_gameTiming.CurTime > component.NextCheck)
            {
                component.Alarming = false;
                component.Passing = false;

                UpdateAppearance((uid, component));
            }
        }
    }

    private void OnCollide(Entity<ScannerGateComponent> entity, ref StartCollideEvent args)
    {
        if (!entity.Comp.Enabled)
            return;

        if (args.OurFixtureId != entity.Comp.FixtureId)
            return;

        if (_gameTiming.CurTime < entity.Comp.NextCheck)
            return;

        TryInvokePort(entity, "GateTrigger");

        entity.Comp.NextCheck = _gameTiming.CurTime + entity.Comp.CheckInterval;

        entity.Comp.Alarming = true;

        if (TryComp<AccessReaderComponent>(entity, out var accessReader))
            if (_accessReader.IsAllowed(args.OtherEntity, entity, accessReader))
            {
                entity.Comp.Passing = true;

                PlayPassed(entity);
                return;
            }

        var checkStatus = CheckItem(args.OtherEntity, entity.Comp.Blacklist, entity.Comp.GrantIgnoreWhitelist);

        entity.Comp.Passing = !checkStatus;

        if (checkStatus)
            PlayDenied(entity);
        else
            PlayPassed(entity);
    }

    private void OnPowerChanged(Entity<ScannerGateComponent> entity, ref PowerChangedEvent args)
    {
        entity.Comp.Enabled = args.Powered;

        UpdateAppearance(entity);
    }

    private void UpdateAppearance(Entity<ScannerGateComponent> entity)
    {
        var finalState = ScannerGateStatusVisualState.Idle;

        if (!entity.Comp.Enabled)
        {
            finalState = ScannerGateStatusVisualState.Off;

        }
        else if (entity.Comp.Alarming)
        {
            if (entity.Comp.Passing)
                finalState = ScannerGateStatusVisualState.Passed;
            else
                finalState = ScannerGateStatusVisualState.Denied;
        }

        _appearanceSystem.SetData(entity, ScannerGateVisualLayers.Status, finalState);
    }

    private void TryInvokePort(EntityUid uid, string port)
    {
        if (HasComp<DeviceLinkSourceComponent>(uid))
            _link.InvokePort(uid, port);
    }

    private void PlayDenied(Entity<ScannerGateComponent> entity)
    {
        TryInvokePort(entity, "GateDenied");

        _audio.PlayPvs(entity.Comp.CheckDeniedSound, entity);
        UpdateAppearance(entity);
    }

    private void PlayPassed(Entity<ScannerGateComponent> entity)
    {
        TryInvokePort(entity, "GatePassed");

        _audio.PlayPvs(entity.Comp.CheckPassedSound, entity);
        UpdateAppearance(entity);
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
