using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.BattleRoyale.DynamicRange;

/// <summary>
/// Dynamic version of RestrictedRangeComponent that works when added after MapInitEvent
/// and updates the boundary when Range is changed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DynamicRangeComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 78f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Origin;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinOriginX = -10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxOriginX = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinOriginY = -10f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxOriginY = 10f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsShrinking = false;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float ShrinkTime = 100f; // Time in seconds to shrink fully

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinimumRange = 5f; // Smallest possible range

    [DataField]
    public bool Processed;

    [DataField]
    public bool OriginInitialized;

    [DataField]
    public float? InitialRange;

    [DataField]
    public TimeSpan? ShrinkStartTime;

    /// <summary>
    /// Dictionary to track the last time damage was applied to each player.
    /// </summary>
    [DataField("lastDamageTimes")]
    public Dictionary<EntityUid, TimeSpan> LastDamageTimes = new();
}
