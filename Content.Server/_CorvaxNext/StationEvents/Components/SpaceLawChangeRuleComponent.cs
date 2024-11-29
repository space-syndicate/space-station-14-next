using Content.Server._CorvaxNext.StationEvents.Events;
using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxNext.StationEvents.Components
{
    [RegisterComponent, Access(typeof(SpaceLawChangeRule))]
    public sealed partial class SpaceLawChangeRuleComponent : Component
    {
        /// <summary>
        /// Localization key of a random message selected for the current event
        /// </summary>
        /// <remarks>
        /// Do not set an initial value for this field!
        /// </remarks>
        [DataField]
        public string? RandomMessage { get; set; }

        /// <summary>
        /// A localized dataset containing the initial list of all laws for the event
        /// </summary>
        [DataField]
        public ProtoId<LocalizedDatasetPrototype> LawLocalizedDataset { get; set; }

        /// <summary>
        /// Time before changes to the law come into force.
        /// Necessary for establish the delay in sending information about the law coming into force
        /// </summary>
        [DataField]
        public int AdaptationTime { get; set; } = 10;
    }
}
