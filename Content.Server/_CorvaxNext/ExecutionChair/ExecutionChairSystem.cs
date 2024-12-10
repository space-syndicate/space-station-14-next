using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Electrocution;
using Content.Server.Power.EntitySystems;
using Content.Shared.Buckle.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server._CorvaxNext.ExecutionChair;

namespace Content.Server._CorvaxNext.ExecutionChair
{
    /// <summary>
    /// This system manages the logic and state of the Execution Chair entity, including responding to
    /// incoming signals, applying electrocution damage to entities strapped into it, and handling sound
    /// and popups when it activates or deactivates.
    /// </summary>
    public sealed partial class ExecutionChairSystem : EntitySystem
    {
        // Dependencies automatically resolved by the IoC container.
        [Dependency] private readonly IGameTiming _gameTimer = default!;
        [Dependency] private readonly IRobustRandom _randomGen = default!;
        [Dependency] private readonly DeviceLinkSystem _deviceSystem = default!;
        [Dependency] private readonly ElectrocutionSystem _shockSystem = default!;
        [Dependency] private readonly SharedAudioSystem _soundSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        // Volume variation range for the shock sound effects to add some randomness.
        private const float VolumeVariationMin = 0.8f;
        private const float VolumeVariationMax = 1.2f;

        /// <summary>
        /// Initializes the system and sets up event subscriptions for when the chair is spawned
        /// and when signals are received (e.g., toggle, on, off) from a device network.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            SetupEventSubscriptions();
        }

        /// <summary>
        /// Subscribes the system to relevant local events:
        ///  - MapInitEvent: when the chair is placed on the map, ensuring the correct device ports.
        ///  - SignalReceivedEvent: when the chair receives device link signals to turn on/off or toggle.
        /// </summary>
        private void SetupEventSubscriptions()
        {
            SubscribeLocalEvent<ExecutionChairComponent, MapInitEvent>(OnChairSpawned);
            SubscribeLocalEvent<ExecutionChairComponent, SignalReceivedEvent>(OnSignalReceived);
        }

        /// <summary>
        /// Called when the Execution Chair is initialized on the map. Ensures that the chair's
        /// device link ports (Toggle, On, Off) are correctly created so it can receive signals.
        /// </summary>
        private void OnChairSpawned(EntityUid uid, ExecutionChairComponent component, ref MapInitEvent args)
        {
            // Ensure that all required device ports are available for linking.
            _deviceSystem.EnsureSinkPorts(uid, component.TogglePort, component.OnPort, component.OffPort);
        }

        /// <summary>
        /// Called when the Execution Chair receives a signal from linked devices.
        /// Depending on the port signaled, the chair will toggle, turn on, or turn off.
        /// Any unexpected port signals are logged.
        /// </summary>
        private void OnSignalReceived(EntityUid uid, ExecutionChairComponent component, ref SignalReceivedEvent args)
        {
            var portSignal = args.Port;

            // Determine new state based on received signal.
            var newState = portSignal switch
            {
                var p when p == component.TogglePort => !component.Enabled,
                var p when p == component.OnPort => true,
                var p when p == component.OffPort => false,
                _ => component.Enabled // If port does not match expected, state remains unchanged.
            };

            // Log a debug message if the port signal is unexpected.
            if (portSignal != component.TogglePort && portSignal != component.OnPort && portSignal != component.OffPort)
            {
                Logger.DebugS("execution_chair", $"Received unexpected port signal: {portSignal} on chair {ToPrettyString(uid)}");
            }

            // Update the chair state based on the new determined state.
            UpdateChairState(uid, newState, component);
        }

        /// <summary>
        /// Updates the Execution Chair's active state (enabled or disabled), synchronizes that state,
        /// and shows a popup message indicating the new state to nearby players.
        /// </summary>
        private void UpdateChairState(EntityUid uid, bool activated, ExecutionChairComponent? component = null)
        {
            // Resolve the component if not provided, ensuring we have a valid reference.
            if (!Resolve(uid, ref component))
                return;

            component.Enabled = activated;

            // Mark the component as "Dirty" so that any networked clients update their state.
            Dirty(uid, component);

            // Display a popup message to indicate the chair has been turned on or off.
            var message = activated
			    ? Loc.GetString("execution-chair-turn-on")
				: Loc.GetString("execution-chair-chair-turn-off");
				
            _popup.PopupEntity(message, uid, PopupType.Medium);
        }

        /// <summary>
        /// Called each frame (or tick). If a chair is active, powered, anchored, and has entities strapped in,
        /// it attempts to electrocute those entities at regular intervals.
        /// </summary>
        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            ProcessActiveChairs();
        }

        /// <summary>
        /// Iterates over all Execution Chairs currently in the game.
        /// For each chair, if it is enabled, anchored, and powered, and if the time has come for the next damage tick,
        /// applies an electrocution effect to all buckled entities.
        /// </summary>
        private void ProcessActiveChairs()
        {
            var query = EntityQueryEnumerator<ExecutionChairComponent>();

            // Process each chair found in the world.
            while (query.MoveNext(out var uid, out var chair))
            {
                // Validate that the chair can operate (is anchored, powered, enabled, and ready for next damage tick).
                if (!ValidateChairOperation(uid, chair))
                    continue;

                // Check if the chair has a StrapComponent and actually has entities buckled to it.
                if (!TryComp<StrapComponent>(uid, out var restraint) || restraint.BuckledEntities.Count == 0)
                    continue;

                // Apply shock damage and effects to all entities buckled into the chair.
                ApplyShockEffect(uid, chair, restraint);
            }
        }

        /// <summary>
        /// Ensures that the chair is in a valid state to operate:
        ///  - The chair is anchored in the world (not picked up or moved).
        ///  - The chair is powered.
        ///  - The chair is currently enabled/turned on.
        ///  - The current game time has passed beyond the next scheduled damage tick.
        /// </summary>
        private bool ValidateChairOperation(EntityUid uid, ExecutionChairComponent chair)
        {
            var transformComponent = Transform(uid);
            return transformComponent.Anchored &&
                   this.IsPowered(uid, EntityManager) &&
                   chair.Enabled &&
                   _gameTimer.CurTime >= chair.NextDamageTick;
        }

        /// <summary>
        /// Attempts to electrocute all entities currently strapped to the chair, causing them damage.
        /// If successful, plays shock sound effects (if configured).
        /// After applying the shocks, sets the next damage tick to one second later.
        /// </summary>
        private void ApplyShockEffect(EntityUid uid, ExecutionChairComponent chair, StrapComponent restraint)
        {
            // Calculate the duration for which each shock is applied.
            var shockDuration = TimeSpan.FromSeconds(chair.DamageTime);

            // For each buckled entity, try to perform an electrocution action.
            foreach (var target in restraint.BuckledEntities)
            {
                // Randomize volume a bit to make each shock sound slightly different.
                var volumeModifier = _randomGen.NextFloat(VolumeVariationMin, VolumeVariationMax);

                // Attempt to electrocute the target. Ignore insulation to ensure damage.
                var shockSuccess = _shockSystem.TryDoElectrocution(
                    target,
                    uid,
                    chair.DamagePerTick,
                    shockDuration,
                    true,
                    volumeModifier,
                    ignoreInsulation: true);

                // If the shock was applied and chair is configured to play sounds, play shock sound.
                if (shockSuccess && chair.PlaySoundOnShock && chair.ShockNoises != null)
                {
                    var audioParams = AudioParams.Default.WithVolume(chair.ShockVolume);
                    _soundSystem.PlayPvs(chair.ShockNoises, target, audioParams);
                }
            }

            // Schedule the next damage tick one second in the future.
            chair.NextDamageTick = _gameTimer.CurTime + TimeSpan.FromSeconds(1);
        }
    }
}
