using Content.Server.Body.Systems;
using Content.Shared._CorvaxNext.Body.Components;

namespace Content.Server.Body.Components
{
    [RegisterComponent, Access(typeof(BrainSystem))]
    public sealed partial class BrainComponent : SharedBrainComponent // Corvax-Next-Surgery
    {
        /// <summary>
        ///     CorvaxNext Change: Is this brain currently controlling the entity?
        /// </summary>
        [DataField]
        public bool Active = true;
    }
}
