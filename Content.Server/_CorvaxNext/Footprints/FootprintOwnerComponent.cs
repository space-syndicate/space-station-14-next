using Content.Shared.FixedPoint;

namespace Content.Server._CorvaxNext.Footprints;

[RegisterComponent]
public sealed partial class FootprintOwnerComponent : Component
{
    [DataField]
    public FixedPoint2 MaxFootVolume = 10;

    [DataField]
    public FixedPoint2 MaxBodyVolume = 20;

    [DataField]
    public float FootDistance = 0.5f;

    [DataField]
    public float BodyDistance = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Distance;

    [DataField]
    public float NextFootOffset = 0.0625f;
}
