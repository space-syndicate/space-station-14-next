using Content.Shared.FixedPoint;
using Content.Shared.RPD.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.RPD.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RPDAmmoSystem))]
public sealed partial class RPDAmmoComponent : Component
{
    /// <summary>
    /// How many charges are contained in this ammo cartridge.
    /// Can be partially transferred into an RCD, until it is empty then it gets deleted.
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Charges = 50;
}

public sealed partial class AdvRPDAmmoComponent : Component
{
    /// <summary>
    /// How many charges are contained in this ammo cartridge.
    /// Can be partially transferred into an RCD, until it is empty then it gets deleted.
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Charges = 250;
}
