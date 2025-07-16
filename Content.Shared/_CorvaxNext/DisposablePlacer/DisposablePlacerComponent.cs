using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._CorvaxNext.DisposablePlacer;

[RegisterComponent]
public sealed partial class DisposablePlacerComponent : Component
{
    [DataField]
    public bool IsPlacing = false;

    [DataField(required: true)]
    public List<EntProtoId> SelectablePrototypes = [];

    [DataField]
    public EntProtoId? Effect;

    [DataField]
    public SoundPathSpecifier? Sound;

    public EntityUid? PlacingEffectUid;
    public EntityUid? PlacingSoundUid;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public float Delay = 5f;

    [DataField]
    public EntProtoId? Prototype;
}
