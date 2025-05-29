using Content.Shared.FixedPoint;
using Content.Shared.RPD.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.RPD.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RPDSystem))]
public sealed partial class RPDDeconstructableComponent : Component
{
    /// <summary>
    /// Number of charges consumed when the deconstruction is completed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Cost = 1;

    /// <summary>
    /// The length of the deconstruction-
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 1f;

    /// <summary>
    /// The visual effect that plays during deconstruction
    /// </summary>
    [DataField("fx"), ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? Effect = null;

    /// <summary>
    /// Toggles whether this entity is deconstructable or not
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Deconstructable = false;


    /// <summary>
    /// Toggles whether this entity is deconstructable by the RPD or not
    /// </summary>
    [DataField("rpd"), ViewVariables(VVAccess.ReadWrite)]
    public bool RpdDeconstructable = true;
}
