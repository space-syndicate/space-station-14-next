using Content.Shared.Coordinates;

namespace Content.Server._CorvaxNext.Botany.Systems;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Server.Botany.Systems;
using Content.Server.Botany.Components;
using Robust.Shared.Timing;
using Content.Server._CorvaxNext.Botany.Components;

/// <summary>
/// This handles...
/// </summary>
public sealed class SeedLungSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolderSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    public static string SeedLungSoluotionName = "SeedLung";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantHolderComponent, SeedLungComponent>();
        while (query.MoveNext(out var uid, out var plantHolder, out var lung))
        {
            if (plantHolder.NextUpdate > _gameTiming.CurTime)
                continue;
            _atmos.Merge(lung.Air, _atmos.GetTileMixture(uid)!);

            if (!_solutionContainerSystem.ResolveSolution(uid, plantHolder.SoilSolutionName, ref plantHolder.SoilSolution, out var solution))
                continue;

            GasToReagent(lung.Air.RemoveVolume(2000f), solution);
            _plantHolderSystem.UpdateReagents(uid, plantHolder);
        }
    }

    private void GasToReagent(GasMixture gas, Solution solution)
    {
        foreach (var gasId in Enum.GetValues<Gas>())
        {
            var i = (int) gasId;
            var moles = gas[i];
            if (moles <= 0)
                continue;

            var reagent = _atmos.GasReagents[i];
            if (reagent is null)
                continue;

            var amount = moles * Atmospherics.SeedBreathMolesToReagentMultiplier;
            solution.AddReagent(reagent, amount);
        }
    }
}

