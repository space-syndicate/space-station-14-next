namespace Content.Server.Traits.Assorted
{
    [RegisterComponent]
    public sealed partial class HeightComponent : Component
    {
        [DataField("tall")]
        public bool Tall = false;

        [DataField("short")]
        public bool Short = false;

        public readonly float TallScale = 1.10f;
        public readonly float ShortScale = 0.90f;

    }
}
