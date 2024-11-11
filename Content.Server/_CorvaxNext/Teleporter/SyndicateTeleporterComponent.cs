using Content.Shared.Physics;

namespace Content.Server._CorvaxNext.Teleporter;

[RegisterComponent]
public sealed partial class SyndicateTeleporterComponent : Component
{
    [DataField]
    public float TeleportationRangeStart = 4;

    [DataField]
    public int TeleportationRangeLength = 4;

    [DataField]
    public CollisionGroup CollisionGroup = CollisionGroup.MobMask;
}
