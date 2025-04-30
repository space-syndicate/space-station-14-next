using Content.Server.Botany;
using Content.Server.Botany.Systems;
using Content.Server.EntityEffects.Effects.PlantMetabolism;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server._CorvaxNext.EntityEffects.Effects.PlantsMetabolism;

[UsedImplicitly]
[DataDefinition]
public sealed partial class PlantHarviumEffects : EntityEffect
{
    [DataField]
    public int PotencyLimit = 50;

    [DataField]
    public int PotencySeedlessThreshold = 30;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!PlantAdjustAttribute.CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager))
            return;

        if (plantHolderComp.Seed == null)
            return;

        var plantHolder = args.EntityManager.System<PlantHolderSystem>();
        plantHolder.EnsureUniqueSeed(args.TargetEntity, plantHolderComp);

        plantHolderComp.Seed!.HarvestRepeat = HarvestType.SelfHarvest;
        plantHolderComp.Seed.Ligneous = true;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => "TODO";

}
