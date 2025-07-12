using Content.Shared._CorvaxNext.QuantumTelepad;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server._CorvaxNext.QuantumTelepad;

public sealed partial class QuantumTelepadSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLinkSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    private bool TryGetFirstQuantumConnection(EntityUid entity, out EntityUid? connectedEntUid)
    {
        connectedEntUid = null;

        if (!TryComp<DeviceLinkSourceComponent>(entity, out var deviceLinkSink))
            return false;

        foreach (var linkedPort in deviceLinkSink.LinkedPorts)
            foreach (var port in linkedPort.Value)
                if (port.Source == "QuantumTelepad")
                {
                    connectedEntUid = linkedPort.Key;
                    return true;
                }

        return false;
    }

    private void StartTeleport(Entity<QuantumTelepadComponent> entity, bool ignoreDelay = false)
    {
        if (!ignoreDelay && _timing.CurTime < entity.Comp.NextTeleport)
        {
            _popup.PopupEntity("quantum-telepad-recharging", entity);
            return;
        }

        if (!TryGetFirstQuantumConnection(entity, out var connectionUid))
        {
            _popup.PopupEntity("quantum-telepad-device-not-found", entity);
            return;
        }

        if (!TryComp<QuantumTelepadComponent>(connectionUid, out var connectedTelepadComp))
            return;

        var senderTransform = Transform(entity);
        var receiverTransform = Transform(connectionUid.Value);

        if (entity.Comp.MustBeAnchored && !senderTransform.Anchored)
            return;

        if (connectedTelepadComp.MustBeAnchored && !receiverTransform.Anchored)
            return;

        if (!_xform.InRange(senderTransform.Coordinates, receiverTransform.Coordinates, entity.Comp.MaxTeleportDistance))
        {
            _popup.PopupEntity("quantum-telepad-out-of-range", entity);
            return;
        }

        var lookupEntities = _lookup.GetEntitiesInRange(entity, entity.Comp.WorkingRange, flags: entity.Comp.LookupFlag);

        if (lookupEntities.Count == 0)
            return;

        var sendedEnts = 0;

        foreach (var lookupEntity in lookupEntities)
        {
            if (sendedEnts > entity.Comp.MaxEntitiesToTeleportAtOnce)
                break;

            if (entity.Comp.Blacklist is not null && _whitelist.IsBlacklistPass(entity.Comp.Blacklist, lookupEntity))
                return;

            if (entity.Comp.Whitelist is not null && _whitelist.IsWhitelistFail(entity.Comp.Whitelist, lookupEntity))
                return;

            _xform.SetWorldPosition(lookupEntity, receiverTransform.Coordinates.Position);

            sendedEnts++;
        }

        if (sendedEnts == 0) // check for any teleported ent
            return;

        if (entity.Comp.TeleportSound is not null)
            _audio.PlayPvs(entity.Comp.TeleportSound, entity);
    }
}
