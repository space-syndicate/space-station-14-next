using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.QuantumTelepad;

[RegisterComponent]
public sealed partial class QuantumTelepadComponent : Component
{
    /// <summary>
    /// Recharge time to teleport again
    /// </summary>
    [DataField]
    public float Delay = 2f;

    /// <summary>
    /// How much time need to wait before next teleport
    /// </summary>
    [DataField]
    public TimeSpan NextTeleport;

    /// <summary>
    /// Whitelist of allowed to teleport entities
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Blacklist of restricted to teleport entities
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

    /// <summary>
    /// Current telepad status
    /// </summary>
    [DataField]
    public TelepadState State = TelepadState.Unpowered;

    /// <summary>
    /// Maximum distance of connection to another telepad
    /// </summary>
    [DataField]
    public float MaxTeleportDistance = 10;

    [DataField]
    public float MaxEntitiesToTeleportAtOnce = 3;

    /// <summary>
    /// Search range for entities to teleport
    /// </summary>
    [DataField]
    public float WorkingRange = 1;

    /// <summary>
    /// Flag to query entities
    /// </summary>
    [DataField("flag")]
    public LookupFlags LookupFlag = LookupFlags.Dynamic;

    /// <summary>
    /// Can be activated by close interaction
    /// </summary>
    [DataField]
    public bool WorksOnInteract = true;

    [DataField]
    public bool MustBeAnchored = true;
}

[Serializable, NetSerializable]
public enum TelepadState : byte
{
    Unpowered,
    Idle,
    Teleporting,
    Recharging,
};
