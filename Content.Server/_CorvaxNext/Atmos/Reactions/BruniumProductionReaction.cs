using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._CorvaxNext.Atmos.Reactions;

/// <summary>
///     Produces brunium from n2 and tritium, with nitric oxide as a catalyst.
///     Efficiency function is modulus function with negative coef. Most effective at 5 atm.
/// </summary>
[UsedImplicitly]
public sealed partial class BruniumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialTrit = mixture.GetMoles(Gas.Tritium);
        var initialN2 = mixture.GetMoles(Gas.Nitrogen);
        var initialN2O = mixture.GetMoles(Gas.NitrousOxide);

        var efficiency = -0.000197385f * Math.Abs(mixture.Pressure - (Atmospherics.OneAtmosphere * 5)) + 1;
        efficiency = efficiency > 0 ? efficiency : 0;
        var loss = 1 - efficiency;

        var catalystLimit = initialN2O * (Atmospherics.BruniumProductionN2ORatio / efficiency);
        var n2Limit = Math.Min(initialN2, catalystLimit) / Atmospherics.BruniumProductionTritRatio;


        var tritUsed = Math.Min(n2Limit, initialTrit);
        var n2Used = tritUsed * Atmospherics.BruniumProductionTritRatio;

        var tritConverted = tritUsed / Atmospherics.BruniumProductionConversionRate;
        var n2Converted = n2Used / Atmospherics.BruniumProductionConversionRate;
        var total = n2Converted + tritConverted;

        mixture.AdjustMoles(Gas.Oxygen, -n2Converted);
        mixture.AdjustMoles(Gas.Tritium, -tritConverted);
        mixture.AdjustMoles(Gas.Brunium, total * efficiency);
        mixture.AdjustMoles(Gas.NitrousOxide, total * loss);

        return efficiency > 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
    }
}
