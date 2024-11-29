using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Fax;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Dataset;
using Robust.Shared.Random;
using Content.Shared.Fax.Components;
using Content.Server.StationEvents.Events;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Server._CorvaxNext.StationEvents.Components;

namespace Content.Server._CorvaxNext.StationEvents.Events
{
    public sealed class SpaceLawChangeRule : StationEventSystem<SpaceLawChangeRuleComponent>
    {
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly FaxSystem _faxSystem = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        /// <summary>
        /// Sequence of laws to be used for the current event
        /// </summary>
        private List<string> _sequenceLaws = new();

        protected override void Started(EntityUid uid, SpaceLawChangeRuleComponent component,
            GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, component, gameRule, args);

            // Loading a prototype dataset
            if (!_prototypeManager.TryIndex<LocalizedDatasetPrototype>(component.LawLocalizedDataset, out var dataset))
            {
                Logger.Error($"LocalizedDatasetPrototype not found: {component.LawLocalizedDataset}");
                return;
            }


            // Initializing the list of laws if it is empty
            if (_sequenceLaws.Count == 0)
            {
                _sequenceLaws.AddRange(dataset.Values);
            }
            // Getting active laws from currently active rules
            var activeLaws = GetActiveSpaceLaws();

            // Excluding active laws from selection
            var availableLaws = _sequenceLaws.Except(activeLaws).ToList();
            if (availableLaws.Count == 0)
            {
                availableLaws = _sequenceLaws;
            }

            // Selecting a random law from the available ones
            var randomLaw = _robustRandom.Pick(availableLaws);
            component.RandomMessage = randomLaw;

            var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);

            var message = Loc.GetString("station-event-space-law-change-announcement",
                ("essence", Loc.GetString(component.RandomMessage)),
                ("time", component.AdaptationTime));

            var faxMessage = Loc.GetString("station-event-space-law-change-fax-announcement",
                ("faxEssence", message),
                ("stationTime", stationTime.ToString("hh\\:mm\\:ss")));

            // Sending a global announcement
            _chat.DispatchGlobalAnnouncement(message, playSound: true, colorOverride: Color.Gold);
            SendSpaceLawChangeFax(faxMessage);

            // Start a timer to send a confirmation message after AdaptationTime minutes
            Timer.Spawn(TimeSpan.FromMinutes(component.AdaptationTime), () => SendConfirmationMessage(uid));
        }

        /// <summary>
        /// Getting active laws from currently active rules
        /// </summary>
        private List<string> GetActiveSpaceLaws()
        {
            var activeLaws = new List<string>();
            foreach (var rule in _gameTicker.GetActiveGameRules())
            {
                if (_entityManager.TryGetComponent(rule, out SpaceLawChangeRuleComponent? spaceLawComponent))
                {
                    if (!string.IsNullOrEmpty(spaceLawComponent.RandomMessage))
                    {
                        activeLaws.Add(spaceLawComponent.RandomMessage);
                    }
                }
            }
            return activeLaws;
        }

        /// <summary>
        /// Sending a confirmation message about the entry into force of changes in Space Law
        /// </summary>
        private void SendConfirmationMessage(EntityUid uid)
        {
            if (!_entityManager.TryGetComponent(uid, out SpaceLawChangeRuleComponent? component) || component.RandomMessage == null)
            {
                Logger.Error($"Failed to send confirmation message for SpaceLawChangeRule for entity {uid}: Component or RandomMessage is null.");
                return;
            }

            var confirmationMessage = Loc.GetString("station-event-space-law-change-announcement-confirmation", ("essence", Loc.GetString(component.RandomMessage)));
            var faxConfirmationMessage = Loc.GetString("station-event-space-law-change-announcement-fax-confirmation", ("faxEssence", confirmationMessage));
            _chat.DispatchGlobalAnnouncement(confirmationMessage, playSound: true, colorOverride: Color.Gold);
            SendSpaceLawChangeFax(faxConfirmationMessage);
        }

        /// <summary>
        /// Sending a fax announcing changes in Space Law
        /// </summary>
        private void SendSpaceLawChangeFax(string message)
        {
            var printout = new FaxPrintout(
                message,
                Loc.GetString("materials-paper"),
                null,
                null,
                "paper_stamp-centcom",
                new List<StampDisplayInfo>
                {
                    new() { StampedName = Loc.GetString("stamp-component-stamped-name-centcom"), StampedColor = Color.FromHex("#006600") }
                });

            var faxes = _entityManager.EntityQuery<FaxMachineComponent>();
            foreach (var fax in faxes)
            {
                if (!fax.ReceiveStationGoal)
                    continue;
                _faxSystem.Receive(fax.Owner, printout, Loc.GetString("station-event-space-law-change-fax-sender"), fax);
            }
        }
    }
}
