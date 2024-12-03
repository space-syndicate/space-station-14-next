using Content.Shared.FixedPoint;

namespace Content.Shared._CorvaxNext.Footprints.Components;

[RegisterComponent]
public sealed partial class FootprintOwnerComponent : Component
{
    [DataField]
    public FixedPoint2 MaxFootVolume = 10;

    [DataField]
    public FixedPoint2 MaxBodyVolume = 20;
}
