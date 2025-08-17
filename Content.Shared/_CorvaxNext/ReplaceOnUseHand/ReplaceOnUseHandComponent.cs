using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.ReplaceOnUseHand;

[RegisterComponent]
public sealed partial class ReplaceOnUseHandComponent : Component
{
    [DataField]
    public float Delay = 1f;

    [DataField(required: true)]
    public EntProtoId? ReplacingEntity;

    [DataField]
    public SoundPathSpecifier? ReplaceSound;
}

[Serializable, NetSerializable]
public sealed partial class ReplaceOnUseDoAfterEvent : SimpleDoAfterEvent
{

}
