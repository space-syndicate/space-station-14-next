using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles increase or decrease of plant potency.
/// </summary>

public sealed partial class PlantAdjustPotency : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-potency";

    [DataField("limit")]
    public int PotencyLimit { get; set; } = 50;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager))
            return;

        if (plantHolderComp.Seed == null)
            return;

        var plantHolder = args.EntityManager.System<PlantHolderSystem>();

        plantHolder.EnsureUniqueSeed(args.TargetEntity, plantHolderComp);

        plantHolderComp.Seed.Potency = Math.Min(Math.Max(plantHolderComp.Seed.Potency + Amount, 1), PotencyLimit);
    }
}
