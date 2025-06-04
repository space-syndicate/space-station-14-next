using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Lavaland.EntityEffects.EffectConditions;

public sealed partial class PressureThreshold : EventEntityEffectCondition<PressureThreshold>
{
    [DataField]
    public bool WorksOnLavaland = false;

    [DataField]
    public float Min = float.MinValue;

    [DataField]
    public float Max = float.MaxValue;

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return Loc.GetString("reagent-effect-condition-pressure-threshold",
            ("min", Min),
            ("max", Max));
    }
}
