using Content.Shared.Random.Rules;

namespace Content.Server._CorvaxNext.Random.Rules;

public sealed partial class OnLavalandRule : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        return false;
    }
}
