using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.EntityEffects.Effects.PlantMetabolism;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._CorvaxNext.EntityEffects.Effects.PlantsMetabolism;

[UsedImplicitly]
[DataDefinition]
public sealed partial class Harvium : PlantAdjustAttribute
{
    [DataField]
    public int PotencyLimit = 50;

    [DataField]
    public int PotencySeedlessThreshold = 30;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager))
            return;

        var plantHolder = args.EntityManager.System<PlantHolderSystem>();

        plantHolderComp.ImproperLight = true;
        plantHolderComp.Health += .5f * Amount;
        plantHolder.CheckHealth(args.TargetEntity, plantHolderComp);
        plantHolder.AffectGrowth(args.TargetEntity, (int) Amount, plantHolderComp);

        if (plantHolderComp.Seed!.Potency < PotencyLimit)
        {
            plantHolder.EnsureUniqueSeed(args.TargetEntity, plantHolderComp);
            plantHolderComp.Seed.Potency = Math.Min(plantHolderComp.Seed.Potency + Amount, PotencyLimit);
        }
        Console.WriteLine(plantHolderComp.Seed.Potency);
    }
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-growth";
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => "TODO";

}
