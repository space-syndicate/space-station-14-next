using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Warps;

/// <summary>
/// Allows ghosts etc to warp to this entity by name.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WarpPointComponent : Component
{
    // Corvax-Next-Warper-Start: Unique (across all loaded maps) identifier for teleporting to warp points.
    [ViewVariables(VVAccess.ReadWrite), DataField("id")]
    public string? ID { get; set; }
    // Corvax-Next-Warper-End

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public string? Location;

    /// <summary>
    /// If true, ghosts warping to this entity will begin following it.
    /// </summary>
    [DataField]
    public bool Follow;

    /// <summary>
    /// What points should be excluded?
    /// Useful where you want things like a ghost to reach only like CentComm
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
