using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxNext.BluespaceTeleportOnTriggerOnTrigger;

[RegisterComponent]
public sealed partial class BluespaceTeleportOnTriggerComponent : Component
{
    [DataField]
    public int Range = 6;

    [DataField]
    public float Probability = 0.7f;

    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
