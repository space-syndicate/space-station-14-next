namespace Content.Server._CorvaxNext.Power;

/// <summary>
/// This is used for wireless Power transition.
/// </summary>
[RegisterComponent]
public sealed partial class PowerTransitComponent : Component
{
    [DataField]
    public Entity<PowerTransitComponent>? LinkedPair;
}
