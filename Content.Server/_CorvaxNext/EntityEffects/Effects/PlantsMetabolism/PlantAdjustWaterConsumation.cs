using Content.Server.EntityEffects.Effects.PlantMetabolism;
using Content.Shared.EntityEffects;

namespace Content.Server._CorvaxNext.EntityEffects.Effects.PlantsMetabolism;

/// <summary>
/// This handles...
/// </summary>
public sealed partial class PlantAdjustWaterConsumation : PlantAdjustAttribute
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager))
            return;

        plantHolderComp.Seed!.WaterConsumption += Amount;
    }

    public override string GuidebookAttributeName { get; set; } = "lol";
}
