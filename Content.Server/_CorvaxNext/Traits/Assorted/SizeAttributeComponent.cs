using Content.Shared.Cloning;

namespace Content.Server.Next.Traits.Assorted
{
    [RegisterComponent]
    public sealed partial class SizeAttributeComponent : Component, ITransferredByCloning
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField]
        public bool Short = false;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField]
        public bool Tall = false;
    }
}
