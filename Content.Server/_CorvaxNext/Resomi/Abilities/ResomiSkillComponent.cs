using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._CorvaxNext.Resomi.Abilities;

[RegisterComponent]
public sealed partial class ResomiSkillComponent : Component
{
    /// <summary>
    /// Whether this component is active or not.
    /// </summarY>
    [DataField("active")]
    public bool Active = false;

    [DataField("switchAgilityAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? SwitchAgilityAction = "SwitchAgilityAction";

    [DataField("switchAgilityActionEntity")] public EntityUid? SwitchAgilityActionEntity;

    [DataField("staminaDamage")]
    public float StaminaDamage = 10f;

    public float SprintSpeedModifier = 1f;

    [DataField("delay")]
    public double Delay = 1.0;
    public TimeSpan UpdateRate => TimeSpan.FromSeconds(Delay);
    public TimeSpan NextUpdateTime;

    [DataField("Test")]
    public TimeSpan Test;
}
