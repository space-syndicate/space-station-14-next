using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Binary.Components;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.Atmos.Piping.Binary.EntitySystems
{
    [UsedImplicitly]
    public sealed class GasPressurePumpSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GasPressurePumpComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceUpdateEvent>(OnPumpUpdated);
            SubscribeLocalEvent<GasPressurePumpComponent, AtmosDeviceDisabledEvent>(OnPumpLeaveAtmosphere);
            SubscribeLocalEvent<GasPressurePumpComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<GasPressurePumpComponent, ActivateInWorldEvent>(OnPumpActivate);
            SubscribeLocalEvent<GasPressurePumpComponent, PowerChangedEvent>(OnPowerChanged);
            // Bound UI subscriptions
            SubscribeLocalEvent<GasPressurePumpComponent, GasPressurePumpChangeOutputPressureMessage>(OnOutputPressureChangeMessage);
            SubscribeLocalEvent<GasPressurePumpComponent, GasPressurePumpToggleStatusMessage>(OnToggleStatusMessage);

            SubscribeLocalEvent<GasPressurePumpComponent, MapInitEvent>(OnMapInit); // Corvax-Next-AutoPipes
        }

        private void OnInit(EntityUid uid, GasPressurePumpComponent pump, ComponentInit args)
        {
            UpdateAppearance(uid, pump);
        }

        private void OnExamined(EntityUid uid, GasPressurePumpComponent pump, ExaminedEvent args)
        {
            if (!EntityManager.GetComponent<TransformComponent>(uid).Anchored || !args.IsInDetailsRange) // Not anchored? Out of range? No status.
                return;

            if (Loc.TryGetString("gas-pressure-pump-system-examined", out var str,
                    ("statusColor", "lightblue"), // TODO: change with pressure?
                    ("pressure", pump.TargetPressure)
                ))
            {
                args.PushMarkup(str);
            }
        }

        private void OnPowerChanged(EntityUid uid, GasPressurePumpComponent component, ref PowerChangedEvent args)
        {
            UpdateAppearance(uid, component);
        }

        private void OnPumpUpdated(EntityUid uid, GasPressurePumpComponent pump, ref AtmosDeviceUpdateEvent args)
        {
            if (!pump.Enabled
                || (TryComp<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
                || !_nodeContainer.TryGetNodes(uid, pump.InletName, pump.OutletName, out PipeNode? inlet, out PipeNode? outlet))
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return;
            }

            var outputStartingPressure = outlet.Air.Pressure;

            if (outputStartingPressure >= pump.TargetPressure)
            {
                _ambientSoundSystem.SetAmbience(uid, false);
                return; // No need to pump gas if target has been reached.
            }

            if (inlet.Air.TotalMoles > 0 && inlet.Air.Temperature > 0)
            {
                // We calculate the necessary moles to transfer using our good ol' friend PV=nRT.
                var pressureDelta = pump.TargetPressure - outputStartingPressure;
                var transferMoles = (pressureDelta * outlet.Air.Volume) / (inlet.Air.Temperature * Atmospherics.R);

                var removed = inlet.Air.Remove(transferMoles);
                _atmosphereSystem.Merge(outlet.Air, removed);
                _ambientSoundSystem.SetAmbience(uid, removed.TotalMoles > 0f);
            }
        }

        private void OnPumpLeaveAtmosphere(EntityUid uid, GasPressurePumpComponent pump, ref AtmosDeviceDisabledEvent args)
        {
            pump.Enabled = false;
            UpdateAppearance(uid, pump);

            DirtyUI(uid, pump);
            _userInterfaceSystem.CloseUi(uid, GasPressurePumpUiKey.Key);
        }

        private void OnPumpActivate(EntityUid uid, GasPressurePumpComponent pump, ActivateInWorldEvent args)
        {
            if (args.Handled || !args.Complex)
                return;

            if (!EntityManager.TryGetComponent(args.User, out ActorComponent? actor))
                return;

            if (Transform(uid).Anchored)
            {
                _userInterfaceSystem.OpenUi(uid, GasPressurePumpUiKey.Key, actor.PlayerSession);
                DirtyUI(uid, pump);
            }
            else
            {
                _popup.PopupCursor(Loc.GetString("comp-gas-pump-ui-needs-anchor"), args.User);
            }

            args.Handled = true;
        }

        private void OnToggleStatusMessage(EntityUid uid, GasPressurePumpComponent pump, GasPressurePumpToggleStatusMessage args)
        {
            pump.Enabled = args.Enabled;
            _adminLogger.Add(LogType.AtmosPowerChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} set the power on {ToPrettyString(uid):device} to {args.Enabled}");
            DirtyUI(uid, pump);
            UpdateAppearance(uid, pump);
        }

        private void OnOutputPressureChangeMessage(EntityUid uid, GasPressurePumpComponent pump, GasPressurePumpChangeOutputPressureMessage args)
        {
            pump.TargetPressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);
            _adminLogger.Add(LogType.AtmosPressureChanged, LogImpact.Medium,
                $"{ToPrettyString(args.Actor):player} set the pressure on {ToPrettyString(uid):device} to {args.Pressure}kPa");
            DirtyUI(uid, pump);

        }

        private void DirtyUI(EntityUid uid, GasPressurePumpComponent? pump)
        {
            if (!Resolve(uid, ref pump))
                return;

            _userInterfaceSystem.SetUiState(uid, GasPressurePumpUiKey.Key,
                new GasPressurePumpBoundUserInterfaceState(EntityManager.GetComponent<MetaDataComponent>(uid).EntityName, pump.TargetPressure, pump.Enabled));
        }

        private void UpdateAppearance(EntityUid uid, GasPressurePumpComponent? pump = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref pump, ref appearance, false))
                return;

            bool pumpOn = pump.Enabled && (TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered);
            _appearance.SetData(uid, PumpVisuals.Enabled, pumpOn, appearance);
        }

        /// Corvax-Next-AutoPipes-Start
        private void OnMapInit(EntityUid uid, GasPressurePumpComponent pump, MapInitEvent args)
        {
            if (pump.StartOnMapInit)
            {
                pump.Enabled = true;
                UpdateAppearance(uid, pump);

                DirtyUI(uid, pump);
                _userInterfaceSystem.CloseUi(uid, GasPressurePumpUiKey.Key);
            }
        }
		/// Corvax-Next-AutoPipes-End
    }
}
