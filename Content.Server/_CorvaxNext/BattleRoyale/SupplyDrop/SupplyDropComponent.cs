using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxNext.BattleRoyale.SupplyDrop;

[RegisterComponent, Access(typeof(SupplyDropSystem))]
public sealed partial class SupplyDropComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId CratePrototype = "RoyalCrateSupplyDrop";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int KillsPerDrop = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int CratesPerDrop = 1;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? SpawnEffectPrototype = "JetpackEffect";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int KillCounter = 0;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int TotalDropped = 0;
}
