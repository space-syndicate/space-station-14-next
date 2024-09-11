namespace Content.Server.Traits.Assorted
{
    [RegisterComponent]
    public sealed partial class SizeAttributeWhitelistComponent : Component
    {
        [DataField("short")]
        public bool Short = false;

        [DataField("shortscale")]
        public float ShortScale = 0f;

        [DataField("tall")]
        public bool Tall = false;

        [DataField("tallscale")]
        public float TallScale = 0f;

    }
}
