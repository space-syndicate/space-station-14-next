using Content.Shared.Roles;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._CorvaxNext.BattleRoyal.Rules.Components;

/// <summary>
/// Component for Battle Royale game mode
/// </summary>
[RegisterComponent, Access(typeof(BattleRoyaleRuleSystem))]
public sealed partial class BattleRoyaleRuleComponent : Component
{
    /// <summary>
    /// The gear players will spawn with
    /// </summary>
    [DataField("gear", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Gear = "DeathMatchGear";

    /// <summary>
    /// Time until the round ends after a winner is determined
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RoundEndDelay = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// The winner of the battle royale
    /// </summary>
    [DataField]
    public EntityUid? Victor;
}
