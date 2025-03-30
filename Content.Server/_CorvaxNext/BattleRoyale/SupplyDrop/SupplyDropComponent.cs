using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._CorvaxNext.BattleRoyale.SupplyDrop;

[RegisterComponent, Access(typeof(SupplyDropSystem))]
public sealed partial class SupplyDropComponent : Component
{
    [DataField("cratePrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string CratePrototype = "RoyalCrateSupplyDrop";

    [DataField("killsPerDrop"), ViewVariables(VVAccess.ReadWrite)]
    public int KillsPerDrop = 1;

    [DataField("cratesPerDrop"), ViewVariables(VVAccess.ReadWrite)]
    public int CratesPerDrop = 1;

    [DataField("spawnEffectPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string? SpawnEffectPrototype = "JetpackEffect";

    [DataField("maxSpawnAttempts"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxSpawnAttempts = 50;

    [DataField("spawnCheckRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float SpawnCheckRadius = 0.5f;

    [DataField("dynamicRangeSpawnMargin"), ViewVariables(VVAccess.ReadWrite)]
    public float DynamicRangeSpawnMargin = 0.8f;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int KillCounter = 0;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int TotalDropped = 0;
}
