using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._CorvaxNext.DisposablePlacer;

[Serializable, NetSerializable]
public sealed class DisposablePlacerSelectMessage : BoundUserInterfaceMessage
{
    public EntProtoId ProtoId;

    public DisposablePlacerSelectMessage(EntProtoId protoId)
    {
        ProtoId = protoId;
    }
}

[Serializable, NetSerializable]
public enum DisposablePlacerSelectRadioUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed partial class DisposablePlacerSelectDoAfterEvent : SimpleDoAfterEvent
{

}
