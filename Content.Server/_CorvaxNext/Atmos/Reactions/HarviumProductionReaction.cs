using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;

namespace Content.Server._CorvaxNext.Atmos.Reactions;

public sealed partial class HarviumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var initialFrezon = mixture.GetMoles(Gas.Frezon);
        var initialO2 = mixture.GetMoles(Gas.Oxygen);
        var initialVapor = mixture.GetMoles(Gas.WaterVapor);

        var efficiency = -0.0006878f*Math.Abs(mixture.Temperature-20500)+1;
        efficiency = efficiency > 0 ? efficiency : 0;

        var usedVapor = Math.Min(Math.Min(initialVapor, initialFrezon * Atmospherics.HarviumProductionVaporFrezonRatio),
            Math.Min(initialFrezon * Atmospherics.HarviumProductionVaporFrezonRatio, initialO2 * Atmospherics.HarviumProductionVaporO2Ratio))
            * efficiency;
        var usedO2 = usedVapor / Atmospherics.HarviumProductionVaporO2Ratio;
        var usedFrezon = usedVapor / Atmospherics.HarviumProductionVaporFrezonRatio;

        var convertedVapor = usedVapor * Atmospherics.HarviumProductionConversionRate ;
        var convertedO2 = usedO2 * Atmospherics.HarviumProductionConversionRate;
        var convertedFrezon = usedFrezon * Atmospherics.HarviumProductionConversionRate;
        var total = convertedFrezon + convertedO2 + convertedFrezon;

        mixture.AdjustMoles(Gas.Harvium, total);
        mixture.AdjustMoles(Gas.Frezon, -convertedFrezon);
        mixture.AdjustMoles(Gas.Oxygen, -convertedO2);
        mixture.AdjustMoles(Gas.WaterVapor, -convertedVapor);
        mixture.Temperature -= 2*efficiency;

        return efficiency > 0 ? ReactionResult.Reacting : ReactionResult.NoReaction;
    }
}
