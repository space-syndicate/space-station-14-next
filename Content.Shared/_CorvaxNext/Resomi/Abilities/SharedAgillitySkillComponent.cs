using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._CorvaxNext.Resomi.Abilities;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class AgillitySkillComponent : Component
{
    [AutoNetworkedField, DataField]
    public Dictionary<string, int> DisabledJumpUpFixtureMasks = new();
    [AutoNetworkedField, DataField]
    public Dictionary<string, int> DisabledJumpDownFixtureMasks = new();

    [DataField("active")]
    public bool Active = false;
    [DataField("jumpEnabled")]
    public bool JumpEnabled = true; // if we want the ability to not give the opportunity to jump on the tables and only accelerate

    [DataField("switchAgilityAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? SwitchAgilityAction = "SwitchAgilityAction";

    [DataField("switchAgilityActionEntity")] public EntityUid? SwitchAgilityActionEntity;

    [DataField("staminaDamageOnJump")]
    public float StaminaDamageOnJump = 10f; //how much stamina will be spent for each jump

    [DataField("staminaDamagePassive")]
    public float StaminaDamagePassive = 3f; //how much stamina will be passive spent while abilitty is activated

    [DataField("sprintSpeedModifier")]
    public float SprintSpeedModifier = 0.1f; //+10%
    public float SprintSpeedCurrent = 1f;

    [DataField("delay")]
    public double Delay = 1.0; // once in how many seconds is our stamina taken away while the ability is on
    public TimeSpan UpdateRate => TimeSpan.FromSeconds(Delay);
    public TimeSpan NextUpdateTime;

    [DataField("cooldown")]
    public double Cooldown = 10.0; //cooldown of ability. Called when the ability is disabled
    public TimeSpan CooldownDelay => TimeSpan.FromSeconds(Cooldown);
}
