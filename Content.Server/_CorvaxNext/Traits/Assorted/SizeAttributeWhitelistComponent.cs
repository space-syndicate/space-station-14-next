namespace Content.Server.Next.Traits.Assorted
{
    [RegisterComponent]
    public sealed partial class SizeAttributeWhitelistComponent : Component
    {
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("short")]
        public bool Short = false;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("shortscale")]
        public float ShortScale = 0f;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("tall")]
        public bool Tall = false;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("tallscale")]
        public float TallScale = 0f;

    }
}
