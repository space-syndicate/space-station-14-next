using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.BattleRoyale.DynamicRange;

/// <summary>
/// Dynamic version of RestrictedRangeComponent that works when added after MapInitEvent
/// and updates boundary when Range is changed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DynamicRangeComponent : Component
{
    // Initial range
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 78f;

    // Current origin point
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public Vector2 Origin;
    
    // Random origin boundaries
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinOriginX = -10f;
    
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxOriginX = 10f;
    
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinOriginY = -10f;
    
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxOriginY = 10f;

    // Shrinking controls
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsShrinking = false;
    
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float ShrinkTime = 100f; // Time in seconds to shrink fully
    
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinimumRange = 5f; // Smallest possible range
    
    // Tracking fields
    [DataField]
    public bool Processed;
    
    [DataField]
    public bool OriginInitialized;
    
    [DataField]
    public float? InitialRange;
    
    [DataField]
    public TimeSpan? ShrinkStartTime;
}
