using Content.Shared.Roles;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Network;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
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
    /// Music that plays when the shrinking zone is close to completion
    /// </summary>
    [DataField]
    public SoundSpecifier ShrinkMusic = new SoundCollectionSpecifier("NukeMusic");

    /// <summary>
    /// Buffer time between music start and zone completion
    /// </summary>
    [DataField]
    public float MusicBuffer = 1.5f;

    /// <summary>
    /// Whether the music has been played for the current shrink cycle
    /// </summary>
    [DataField]
    public bool PlayedShrinkMusic = false;

    /// <summary>
    /// The entity with the DynamicRange component
    /// </summary>
    [DataField]
    public EntityUid? DynamicRangeEntity;

    /// <summary>
    /// The winner of the battle royale
    /// </summary>
    [DataField]
    public EntityUid? Victor;
    
    /// <summary>
    /// Current shrink cycle index
    /// </summary>
    [DataField]
    public int ShrinkCycle = 0;
}
