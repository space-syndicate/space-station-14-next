using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Light.Components;

namespace Content.Server.Light.EntitySystems;

/// <summary>
///     Handles the logic between signals and toggling OccluderComponent
/// </summary>
public sealed class ToggleableOccluderSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly OccluderSystem _occluder = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ToggleableOccluderComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ToggleableOccluderComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, ToggleableOccluderComponent comp, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, comp.OnPort, comp.OffPort, comp.TogglePort);
    }

    private void OnSignalReceived(EntityUid uid, ToggleableOccluderComponent comp, ref SignalReceivedEvent args)
    {
        if (!TryComp<OccluderComponent>(uid, out var occluder))
            return;

        switch (args.Port)
        {
            case comp.OffPort:
                SetState(uid, false, occluder);
                break;

            case comp.OnPort:
                SetState(uid, true, occluder);
                break;

            case comp.TogglePort:
                ToggleState(uid, occluder);
                break;
        }
    }

    public void ToggleState(EntityUid uid, OccluderComponent? occluder = null)
    {
        if (!Resolve(uid, ref occluder))
            return;

        SetState(uid, !occluder.Enabled, occluder);
    }

    public void SetState(EntityUid uid, bool state, OccluderComponent? occluder = null)
    {
        if (!Resolve(uid, ref occluder))
            return;

        _occluder.SetEnabled(uid, state);
    }

}
